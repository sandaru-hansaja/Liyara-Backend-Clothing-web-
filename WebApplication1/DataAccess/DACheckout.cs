using Microsoft.Data.SqlClient;
using WebApplication1.Models;


namespace WebApplication1.DataAccess
{
    public class DACheckout
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public DACheckout(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<int> SaveCustomerAsync(PlaceOrderViewModel dto)
        {
            using var conn = new SqlConnection(_connectionString);
            string query = @"
                INSERT INTO PlaceOrderView (FirstName, LastName, Address, City, PostalCode, Phone, Email, PaymentMethod,Total)
                OUTPUT INSERTED.PlaceOrderViewID
                VALUES (@FirstName, @LastName, @Address, @City, @PostalCode, @Phone, @Email, @PaymentMethod,@Total)";
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", dto.LastName);
            cmd.Parameters.AddWithValue("@Address", dto.Address);
            cmd.Parameters.AddWithValue("@City", dto.City);
            cmd.Parameters.AddWithValue("@PostalCode", dto.PostalCode);
            cmd.Parameters.AddWithValue("@Phone", dto.Phone);
            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@PaymentMethod", dto.PaymentMethod);
            cmd.Parameters.AddWithValue("@Total", dto.Total);

            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<bool> UpdateProductStock(int productId, int quantity)
        {
            using var conn = new SqlConnection(_connectionString);
            string query = @"
                UPDATE Product 
                SET Stock = Stock - @Quantity,
                    Status = CASE WHEN Stock - @Quantity > 0 THEN 'Available' ELSE 'Sold Out' END
                WHERE ProductID = @ProductID AND Stock >= @Quantity";
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> PlaceOrderAsync(OrderModel order)
        {
            using var conn = new SqlConnection(_connectionString);
            string orderQuery = @"
                INSERT INTO Orders (Total, OrderDate, PaymentMethod, PlaceOrderViewID, UserID, ShippingAddress)
                OUTPUT INSERTED.OrderID
                VALUES (@Total, @OrderDate, @PaymentMethod, @PlaceOrderViewID, @UserID, @ShippingAddress)";
            using var cmd = new SqlCommand(orderQuery, conn);

            cmd.Parameters.AddWithValue("@Total", order.Total);
            cmd.Parameters.AddWithValue("@OrderDate", order.OrderDate);
            cmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);
            cmd.Parameters.AddWithValue("@PlaceOrderViewID", order.PlaceOrderViewID);
            cmd.Parameters.AddWithValue("@UserID", order.UserID);
            cmd.Parameters.AddWithValue("@ShippingAddress", order.ShippingAddress);

            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task SaveOrderItemsAsync(List<OrderItemModel> items, int orderId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var item in items)
            {
                string query = @"
                    INSERT INTO OrderItem (OrderID, ProductID, Quantity, SubTotal, Color, Size)
                    VALUES (@OrderID, @ProductID, @Quantity, @SubTotal, @Color, @Size)";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                cmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@SubTotal", item.SubTotal);
                cmd.Parameters.AddWithValue("@Color", item.Color ?? "");
                cmd.Parameters.AddWithValue("@Size", item.Size ?? "");

                await cmd.ExecuteNonQueryAsync();
            }
        }

        //public List<object> GetAllOrders()
        //{
        //    var orders = new List<object>();

        //    using var conn = new SqlConnection(_connectionString);
        //    string query = @"
        //        SELECT o.OrderID, p.FirstName, p.LastName, p.Phone, o.Total, o.OrderDate, o.Status
        //        FROM Orders o
        //        JOIN PlaceOrderView p ON o.PlaceOrderViewID = p.PlaceOrderViewID";

        //    using var cmd = new SqlCommand(query, conn);
        //    conn.Open();
        //    using var reader = cmd.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        orders.Add(new
        //        {
        //            OrderID = (int)reader["OrderID"],
        //            CustomerName = $"{reader["FirstName"]} {reader["LastName"]}",
        //            MobileNumber = reader["Phone"].ToString(),
        //            Total = Convert.ToDecimal(reader["Total"]),
        //            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
        //            Status = reader["Status"].ToString()
        //        });
        //    }

        //    return orders;
        //}
        //public List<object> GetAllOrders()
        //{
        //    var orders = new List<object>();

        //    using var conn = new SqlConnection(_connectionString);
        //    string query = @"
        //        SELECT o.OrderID, p.FirstName, p.LastName, p.Phone, o.Total, o.OrderDate, o.Status
        //        FROM Orders o
        //        JOIN PlaceOrderView p ON o.PlaceOrderViewID = p.PlaceOrderViewID";

        //    using var cmd = new SqlCommand(query, conn);
        //    conn.Open();
        //    using var reader = cmd.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        int orderId = reader["OrderID"] != DBNull.Value ? (int)reader["OrderID"] : 0;
        //        string firstName = reader["FirstName"]?.ToString() ?? "";
        //        string lastName = reader["LastName"]?.ToString() ?? "";
        //        string phone = reader["Phone"]?.ToString() ?? "N/A";
        //        decimal total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0;
        //        DateTime orderDate = reader["OrderDate"] != DBNull.Value ? Convert.ToDateTime(reader["OrderDate"]) : DateTime.MinValue;
        //        string status = reader["Status"]?.ToString() ?? "Unknown";

        //        orders.Add(new
        //        {
        //            OrderID = orderId,
        //            CustomerName = $"{firstName} {lastName}".Trim(),
        //            MobileNumber = phone,
        //            Total = total,
        //            OrderDate = orderDate,
        //            Status = status
        //        });
        //    }

        //    return orders;
        //}
        public async Task<List<object>> GetAllOrdersAsync()
        {
            var orders = new List<object>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                string query = @"
            SELECT o.OrderID, p.FirstName, p.LastName, p.Phone, o.Total, o.OrderDate, o.Status
            FROM Orders o
            JOIN PlaceOrderView p ON o.PlaceOrderViewID = p.PlaceOrderViewID";

                using var cmd = new SqlCommand(query, conn);
                await conn.OpenAsync(); //  async open

                using var reader = await cmd.ExecuteReaderAsync(); //  async read

                while (await reader.ReadAsync()) //  async loop
                {
                    int orderId = reader["OrderID"] != DBNull.Value ? (int)reader["OrderID"] : 0;
                    string firstName = reader["FirstName"]?.ToString() ?? "";
                    string lastName = reader["LastName"]?.ToString() ?? "";
                    string phone = reader["Phone"]?.ToString() ?? "N/A";
                    decimal total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0;
                    DateTime orderDate = reader["OrderDate"] != DBNull.Value ? Convert.ToDateTime(reader["OrderDate"]) : DateTime.MinValue;
                    string status = reader["Status"]?.ToString() ?? "Unknown";

                    orders.Add(new
                    {
                        OrderID = orderId,
                        CustomerName = $"{firstName} {lastName}".Trim(),
                        MobileNumber = phone,
                        Total = total,
                        OrderDate = orderDate,
                        Status = status
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 SQL ERROR in GetAllOrdersAsync: " + ex.Message);
                throw;
            }

            return orders;
        }



        public async Task<bool> UpdateOrderStatus(int orderId, string status)
        {
            using var conn = new SqlConnection(_connectionString);
            string query = "UPDATE Orders SET Status = @Status WHERE OrderID = @OrderID";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            cmd.Parameters.AddWithValue("@Status", status);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var tran = conn.BeginTransaction();

            try
            {
                var deleteItems = new SqlCommand("DELETE FROM OrderItem WHERE OrderID = @OrderID", conn, tran);
                deleteItems.Parameters.AddWithValue("@OrderID", orderId);
                await deleteItems.ExecuteNonQueryAsync();

                var deleteOrder = new SqlCommand("DELETE FROM Orders WHERE OrderID = @OrderID", conn, tran);
                deleteOrder.Parameters.AddWithValue("@OrderID", orderId);
                await deleteOrder.ExecuteNonQueryAsync();

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                return false;
            }
        }





        //        public async Task<object?> GetOrderDetailsAsync(int orderId)
        //        {
        //            using var conn = new SqlConnection(_connectionString);
        //            await conn.OpenAsync();

        //            // Step 1: Get order, customer, and shipping details
        //            var orderInfoQuery = @"
        //        SELECT 
        //            o.OrderID,
        //            o.OrderDate,
        //            o.Total,
        //            o.Status,
        //            o.PaymentMethod,
        //            o.ShippingAddress,
        //            p.Email,
        //            p.FirstName,
        //            p.LastName
        //        FROM Orders o
        //        JOIN PlaceOrderView p ON o.PlaceOrderViewID = p.PlaceOrderViewID
        //        WHERE o.OrderID = @OrderID";

        //            using var orderCmd = new SqlCommand(orderInfoQuery, conn);
        //            orderCmd.Parameters.AddWithValue("@OrderID", orderId);
        //            using var orderReader = await orderCmd.ExecuteReaderAsync();

        //            if (!await orderReader.ReadAsync())
        //                return null;

        //            var orderDetails = new
        //            {
        //                OrderID = (int)orderReader["OrderID"],
        //                Email = orderReader["Email"].ToString(),
        //                CustomerName = $"{orderReader["FirstName"]} {orderReader["LastName"]}",
        //                OrderDate = (DateTime)orderReader["OrderDate"],
        //                Total = (decimal)orderReader["Total"],
        //                Status = orderReader["Status"].ToString(),
        //                PaymentMethod = orderReader["PaymentMethod"].ToString(),
        //                ShippingAddress = orderReader["ShippingAddress"].ToString(),
        //                Items = new List<object>() // will be filled in Step 2
        //            };

        //            await orderReader.CloseAsync(); // Important: close before running another command

        //            // Step 2: Get order items with product info
        //            var itemQuery = @"
        //                SELECT 
        //                    oi.ProductID,
        //                    p.Name AS ProductName,
        //                    p.Price,
        //                    oi.Quantity
        //                FROM OrderItem oi
        //                JOIN Product p ON oi.ProductID = p.ProductID
        //                WHERE oi.OrderID = @OrderID";

        //            using var itemCmd = new SqlCommand(itemQuery, conn);
        //            itemCmd.Parameters.AddWithValue("@OrderID", orderId);
        //            using var itemReader = await itemCmd.ExecuteReaderAsync();

        //            var items = new List<object>();
        //            while (await itemReader.ReadAsync())
        //            {
        //                items.Add(new
        //                {
        //                    ProductID = (int)itemReader["ProductID"],
        //                    ProductName = itemReader["ProductName"].ToString(),
        //                    Price = (decimal)itemReader["Price"],
        //                    Quantity = (int)itemReader["Quantity"],
        //                    SubTotal = (decimal)itemReader["Price"] * (int)itemReader["Quantity"]
        //                });
        //            }

        //            // Combine order details and items
        //            return new
        //            {
        //                orderDetails.OrderID,
        //                orderDetails.Email,
        //                orderDetails.CustomerName,
        //                orderDetails.OrderDate,
        //                orderDetails.Total,
        //                orderDetails.Status,
        //                orderDetails.PaymentMethod,
        //                orderDetails.ShippingAddress,
        //                Items = items
        //            };
        //        }

        //    }

        //}
        public async Task<object?> GetOrderDetailsAsync(int orderId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Step 1: Get order, customer, and shipping details
            var orderInfoQuery = @"
        SELECT 
            o.OrderID,
            o.OrderDate,
            o.Total,
            o.Status,
            o.PaymentMethod,
            o.ShippingAddress,
            p.Email,
            p.FirstName,
            p.LastName
        FROM Orders o
        JOIN PlaceOrderView p ON o.PlaceOrderViewID = p.PlaceOrderViewID
        WHERE o.OrderID = @OrderID";

            using var orderCmd = new SqlCommand(orderInfoQuery, conn);
            orderCmd.Parameters.AddWithValue("@OrderID", orderId);
            using var orderReader = await orderCmd.ExecuteReaderAsync();

            if (!await orderReader.ReadAsync())
                return null;

            var orderDetails = new
            {
                OrderID = (int)orderReader["OrderID"],
                Email = orderReader["Email"].ToString(),
                CustomerName = $"{orderReader["FirstName"]} {orderReader["LastName"]}",
                OrderDate = (DateTime)orderReader["OrderDate"],
                Total = (decimal)orderReader["Total"],
                Status = orderReader["Status"].ToString(),
                PaymentMethod = orderReader["PaymentMethod"].ToString(),
                ShippingAddress = orderReader["ShippingAddress"].ToString(),
                Items = new List<object>()
            };

            await orderReader.CloseAsync(); // Must close before next command

            // Step 2: Get order items with product info and image path
            var itemQuery = @"
        SELECT 
            oi.ProductID,
            p.Name AS ProductName,
            p.Price,
            oi.Quantity,
            i.ImageData
        FROM OrderItem oi
        JOIN Product p ON oi.ProductID = p.ProductID
        LEFT JOIN ProductImage i ON p.ProductID = i.ProductID
        WHERE oi.OrderID = @OrderID";

            using var itemCmd = new SqlCommand(itemQuery, conn);
            itemCmd.Parameters.AddWithValue("@OrderID", orderId);
            using var itemReader = await itemCmd.ExecuteReaderAsync();

            var items = new List<object>();
            while (await itemReader.ReadAsync())
            {
                string? imageUrl = null;

                if (!itemReader.IsDBNull(itemReader.GetOrdinal("ImageData")))
                {
                    string rawPath = itemReader.GetString(itemReader.GetOrdinal("ImageData"));
                    string relativePath = rawPath.Replace(@"C:\inetpub\wwwroot\liarabackend\wwwroot\uploads\products", "").Replace(@"\", "/");
                    imageUrl = "https://liarabackend.dockyardsoftware.com" + relativePath;
                }

                items.Add(new
                {
                    ProductID = (int)itemReader["ProductID"],
                    ProductName = itemReader["ProductName"].ToString(),
                    Price = (decimal)itemReader["Price"],
                    Quantity = (int)itemReader["Quantity"],
                    SubTotal = (decimal)itemReader["Price"] * (int)itemReader["Quantity"],
                    ImageUrl = imageUrl
                });
            }

            return new
            {
                orderDetails.OrderID,
                orderDetails.Email,
                orderDetails.CustomerName,
                orderDetails.OrderDate,
                orderDetails.Total,
                orderDetails.Status,
                orderDetails.PaymentMethod,
                orderDetails.ShippingAddress,
                Items = items
            };
        }
    }
}
