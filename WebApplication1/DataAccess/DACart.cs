using Microsoft.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.DataAccess
{
    public class DACart
    {
        private readonly string _connectionString;

        public DACart(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<List<CartItemModel>> GetCartItemsByUserId(int userId)
        {
            var cartItems = new List<CartItemModel>();

            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM CartItems WHERE UserID = @UserID";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserID", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cartItems.Add(new CartItemModel
                {
                    ProductID = (int)reader["ProductID"],
                    Quantity = (int)reader["Quantity"],
                    Price = (decimal)reader["Price"],
                    UserID = (int)reader["UserID"]
                });
            }

            return cartItems;
        }

        public async Task SaveCartItems(int userId, List<CartItemModel> cartItems)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var deleteCmd = new SqlCommand("DELETE FROM CartItems WHERE UserID = @UserID", connection);
            deleteCmd.Parameters.AddWithValue("@UserID", userId);
            await deleteCmd.ExecuteNonQueryAsync();

            foreach (var item in cartItems)
            {
                var insertCmd = new SqlCommand(@"
                    INSERT INTO CartItems (ProductID, Quantity, Price, UserID) 
                    VALUES (@ProductID, @Quantity, @Price, @UserID)", connection);

                insertCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                insertCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                insertCmd.Parameters.AddWithValue("@Price", item.Price);
                insertCmd.Parameters.AddWithValue("@UserID", userId);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<OrderModel>> GetUserOrders(int userId)
        {
            var orders = new List<OrderModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var orderQuery = "SELECT * FROM Orders WHERE UserID = @UserID";
            var orderCmd = new SqlCommand(orderQuery, connection);
            orderCmd.Parameters.AddWithValue("@UserID", userId);

            using var orderReader = await orderCmd.ExecuteReaderAsync();

            while (await orderReader.ReadAsync())
            {
                var order = new OrderModel
                {
                    OrderID = (int)orderReader["OrderID"],
                    OrderDate = (DateTime)orderReader["OrderDate"],
                    Total = (decimal)orderReader["Total"],
                    OrderItems = new List<OrderItemModel>()
                };
                orders.Add(order);
            }

            orderReader.Close();

            foreach (var order in orders)
            {
                var itemCmd = new SqlCommand("SELECT * FROM OrderItems WHERE OrderID = @OrderID", connection);
                itemCmd.Parameters.AddWithValue("@OrderID", order.OrderID);

                using var itemReader = await itemCmd.ExecuteReaderAsync();
                while (await itemReader.ReadAsync())
                {
                    order.OrderItems.Add(new OrderItemModel
                    {
                        ProductID = (int)itemReader["ProductID"],
                        Quantity = (int)itemReader["Quantity"],
                        Price = (decimal)itemReader["Price"]
                    });
                }
                itemReader.Close();
            }

            return orders;
        }
    }
}
