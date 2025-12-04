
//using System.Data.SqlClient;
//using WebApplication1.Models;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;

//namespace WebApplication1.DataAccess
//{
//    public class DAReturnRequest
//    {
//        private readonly string _connectionString;

//        public DAReturnRequest(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("DefaultConnection");
//        }


//        public async Task<bool> SubmitReturnAsync(ReturnRequestModel dto)
//        {
//            try
//            {
//                using SqlConnection conn = new SqlConnection(_connectionString);
//                await conn.OpenAsync();

//                // Step 1: Get ProductID from OrderItem based on OrderID
//                string getProductIdQuery = "SELECT TOP 1 ProductID FROM OrderItem WHERE OrderID = @OrderID";
//                using SqlCommand getProductCmd = new SqlCommand(getProductIdQuery, conn);
//                getProductCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);

//                object result = await getProductCmd.ExecuteScalarAsync();
//                if (result == null)
//                    throw new Exception($"No product found for OrderID {dto.OrderID}");

//                int productId = Convert.ToInt32(result);

//                // Step 2: Insert into ReturnRequest with the fetched ProductID and ImageUrl as path
//                string insertQuery = @"INSERT INTO ReturnRequest 
//        (OrderID, ProductID, FullName, Email, PhoneNumber, Reason, ProductCondition, Comment, ImageUrl, Status) 
//        VALUES 
//        (@OrderID, @ProductID, @FullName, @Email, @PhoneNumber, @Reason, @ProductCondition, @Comment, @ImageUrl, @Status)";

//                using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);

//                insertCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);
//                insertCmd.Parameters.AddWithValue("@ProductID", productId);
//                insertCmd.Parameters.AddWithValue("@FullName", dto.FullName);
//                insertCmd.Parameters.AddWithValue("@Email", dto.Email);
//                insertCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
//                insertCmd.Parameters.AddWithValue("@Reason", dto.Reason);
//                insertCmd.Parameters.AddWithValue("@ProductCondition", dto.ProductCondition);
//                insertCmd.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(dto.Comment) ? DBNull.Value : dto.Comment);
//                insertCmd.Parameters.AddWithValue("@ImageUrl", string.IsNullOrEmpty(dto.ImageUrl) ? DBNull.Value : dto.ImageUrl); // << Store file path
//                insertCmd.Parameters.AddWithValue("@Status", dto.Status ?? "Pending");

//                return await insertCmd.ExecuteNonQueryAsync() > 0;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("❌ Error in SubmitReturnAsync: " + ex.Message);
//                throw;
//            }
//        }


//        // Get all return requests
//        public async Task<List<ReturnRequestModel>> GetAllReturnsAsync()
//        {
//            var list = new List<ReturnRequestModel>();
//            using SqlConnection conn = new SqlConnection(_connectionString);
//            string query = "SELECT * FROM ReturnRequest ORDER BY CreatedAt DESC";
//            using SqlCommand cmd = new SqlCommand(query, conn);
//            await conn.OpenAsync();
//            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

//            while (await reader.ReadAsync())
//            {
//                list.Add(MapReturnRequest(reader));
//            }

//            return list;
//        }

//        // Get return requests by OrderID
//        public async Task<List<ReturnRequestModel>> GetReturnsByOrderIdAsync(int orderId)
//        {
//            var list = new List<ReturnRequestModel>();
//            using SqlConnection conn = new SqlConnection(_connectionString);
//            string query = "SELECT * FROM ReturnRequest WHERE OrderID = @OrderID ORDER BY CreatedAt DESC";
//            using SqlCommand cmd = new SqlCommand(query, conn);
//            cmd.Parameters.AddWithValue("@OrderID", orderId);
//            await conn.OpenAsync();

//            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
//            while (await reader.ReadAsync())
//            {
//                list.Add(MapReturnRequest(reader));
//            }

//            return list;
//        }

//        // Get single return request by ReturnID
//        public async Task<ReturnRequestModel?> GetReturnByIdAsync(int returnId)
//        {
//            using SqlConnection conn = new SqlConnection(_connectionString);
//            string sql = "SELECT * FROM ReturnRequest WHERE ReturnID = @ReturnID";
//            using SqlCommand cmd = new SqlCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@ReturnID", returnId);

//            await conn.OpenAsync();
//            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

//            if (await reader.ReadAsync())
//            {
//                return MapReturnRequest(reader);
//            }
//            return null;
//        }

//        // Update return status
//        public async Task<bool> UpdateReturnStatusAsync(int returnId, string newStatus)
//        {
//            using SqlConnection conn = new SqlConnection(_connectionString);
//            string query = "UPDATE ReturnRequest SET Status = @Status WHERE ReturnID = @ReturnID";
//            using SqlCommand cmd = new SqlCommand(query, conn);
//            cmd.Parameters.AddWithValue("@Status", newStatus);
//            cmd.Parameters.AddWithValue("@ReturnID", returnId);
//            await conn.OpenAsync();
//            return await cmd.ExecuteNonQueryAsync() > 0;
//        }


//        public async Task<List<ProductModel>> GetProductsByOrderIdAsync(int orderId)
//        {
//            var products = new List<ProductModel>();

//            var sql = @"
//SELECT p.ProductID, p.Name, p.Price, p.Stock, p.Description
//FROM OrderItem oi
//JOIN Product p ON oi.ProductID = p.ProductID
//WHERE oi.OrderID = @OrderID";

//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@OrderID", orderId);

//            await conn.OpenAsync();
//            using var reader = await cmd.ExecuteReaderAsync();

//            var productList = new List<ProductModel>();

//            while (await reader.ReadAsync())
//            {
//                var product = new ProductModel
//                {
//                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
//                    Name = reader.GetString(reader.GetOrdinal("Name")),
//                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
//                    Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
//                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
//                        ? null
//                        : reader.GetString(reader.GetOrdinal("Description"))
//                };

//                productList.Add(product);
//            }

//            await reader.CloseAsync();

//            // Load images for each product - now loading image paths instead of base64
//            foreach (var product in productList)
//            {
//                var imageCmd = new SqlCommand(@"
//    SELECT ImageData 
//    FROM ProductImage 
//    WHERE ProductID = @ProductID", conn);

//                imageCmd.Parameters.AddWithValue("@ProductID", product.ProductID);

//                using var imageReader = await imageCmd.ExecuteReaderAsync();

//                var imagePaths = new List<string>();

//                while (await imageReader.ReadAsync())
//                {
//                    if (!imageReader.IsDBNull(0))
//                    {
//                        string? imagePath = imageReader.GetString(0);
//                        if (!string.IsNullOrEmpty(imagePath))
//                        {
//                            imagePaths.Add(imagePath);
//                        }
//                    }
//                }

//                await imageReader.CloseAsync();

//                product.ImageUrls = imagePaths;

//                products.Add(product);
//            }

//            return products;
//        }



//        private ReturnRequestModel MapReturnRequest(SqlDataReader reader)
//        {
//            string? rawImage = reader["ImageUrl"] as string;
//            string? relativePath = null;

//            if (!string.IsNullOrEmpty(rawImage))
//            {
//                // Normalize path separators and remove base server path
//                string basePath = @"C:\inetpub\wwwroot\liarabackend\wwwroot\Uploads\Returns";
//                string fileName = Path.GetFileName(rawImage);

//                // Final relative path for frontend
//                relativePath = "/uploads/returns/" + fileName;
//            }

//            return new ReturnRequestModel
//            {
//                ReturnID = reader.GetInt32(reader.GetOrdinal("ReturnID")),
//                OrderID = reader.GetInt32(reader.GetOrdinal("OrderID")),
//                FullName = reader.GetString(reader.GetOrdinal("FullName")),
//                Email = reader.GetString(reader.GetOrdinal("Email")),
//                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
//                Reason = reader.GetString(reader.GetOrdinal("Reason")),
//                ProductCondition = reader.GetString(reader.GetOrdinal("ProductCondition")),
//                Comment = reader["Comment"] as string,
//                ImageUrl = relativePath,
//                Status = reader.GetString(reader.GetOrdinal("Status")),
//                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
//            };
//        }


//    }
//}
using System.Data.SqlClient;
using WebApplication1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace WebApplication1.DataAccess
{
    public class DAReturnRequest
    {
        private readonly string _connectionString;

        public DAReturnRequest(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //public async Task<bool> SubmitReturnAsync(ReturnRequestModel dto)
        //{
        //    try
        //    {
        //        using SqlConnection conn = new SqlConnection(_connectionString);
        //        await conn.OpenAsync();

        //        // Get ProductID from OrderItem based on OrderID
        //        string getProductIdQuery = "SELECT TOP 1 ProductID FROM OrderItem WHERE OrderID = @OrderID";
        //        using SqlCommand getProductCmd = new SqlCommand(getProductIdQuery, conn);
        //        getProductCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);

        //        object result = await getProductCmd.ExecuteScalarAsync();
        //        if (result == null)
        //            throw new Exception($"No product found for OrderID {dto.OrderID}");

        //        int productId = Convert.ToInt32(result);

        //        // Insert into ReturnRequest with the fetched ProductID and ImageUrl (relative path)
        //        string insertQuery = @"
        //            INSERT INTO ReturnRequest 
        //            (OrderID, ProductID, FullName, Email, PhoneNumber, Reason, ProductCondition, Comment, ImageUrl, Status) 
        //            VALUES 
        //            (@OrderID, @ProductID, @FullName, @Email, @PhoneNumber, @Reason, @ProductCondition, @Comment, @ImageUrl, @Status)";

        //        using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);

        //        insertCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);
        //        insertCmd.Parameters.AddWithValue("@ProductID", productId);
        //        insertCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        //        insertCmd.Parameters.AddWithValue("@Email", dto.Email);
        //        insertCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
        //        insertCmd.Parameters.AddWithValue("@Reason", dto.Reason);
        //        insertCmd.Parameters.AddWithValue("@ProductCondition", dto.ProductCondition);
        //        insertCmd.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(dto.Comment) ? DBNull.Value : dto.Comment);
        //        insertCmd.Parameters.AddWithValue("@ImageUrl", string.IsNullOrEmpty(dto.ImageUrl) ? DBNull.Value : dto.ImageUrl); // Should be relative path, e.g. "/uploads/returns/return_123.jpg"
        //        insertCmd.Parameters.AddWithValue("@Status", dto.Status ?? "Pending");

        //        return await insertCmd.ExecuteNonQueryAsync() > 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("❌ Error in SubmitReturnAsync: " + ex.Message);
        //        throw;
        //    }
        //}
        public async Task<bool> SubmitReturnAsync(ReturnRequestModel dto)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                // Step 1: Get ProductID from OrderItem based on OrderID
                string getProductIdQuery = "SELECT TOP 1 ProductID FROM OrderItem WHERE OrderID = @OrderID";
                using SqlCommand getProductCmd = new SqlCommand(getProductIdQuery, conn);
                getProductCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);
                object result = await getProductCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new Exception($"No product found for OrderID {dto.OrderID}");
                int productId = Convert.ToInt32(result);
                // Step 2: Insert into ReturnRequest
                string insertQuery = @"
                INSERT INTO ReturnRequest
                (OrderID,
                ProductID,
                FullName, 
                Email, 
                PhoneNumber,
                Reason,
                ProductCondition, 
                Comment,
                ImageUrl,
                Status)
                VALUES
                (@OrderID, @ProductID, @FullName, @Email, @PhoneNumber, @Reason, @ProductCondition, @Comment, @ImageUrl, @Status)";
                using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);
                insertCmd.Parameters.AddWithValue("@ProductID", productId);
                insertCmd.Parameters.AddWithValue("@FullName", dto.FullName ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Email", dto.Email ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Reason", dto.Reason ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ProductCondition", dto.ProductCondition ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(dto.Comment) ? DBNull.Value : dto.Comment);
                insertCmd.Parameters.AddWithValue("@ImageUrl", string.IsNullOrEmpty(dto.ImageUrl) ? DBNull.Value : dto.ImageUrl);
                insertCmd.Parameters.AddWithValue("@Status", dto.Status ?? "Pending");
                int rowsInserted = await insertCmd.ExecuteNonQueryAsync();
                if (rowsInserted > 0)
                {
                    // Step 3: Fetch the saved Reason (ensure it's the latest)
                    string fetchReasonQuery = @"
                SELECT TOP 1 Reason
                FROM ReturnRequest
                WHERE OrderID = @OrderID AND ProductID = @ProductID
                ORDER BY ReturnID DESC";
                    using SqlCommand fetchReasonCmd = new SqlCommand(fetchReasonQuery, conn);
                    fetchReasonCmd.Parameters.AddWithValue("@OrderID", dto.OrderID);
                    fetchReasonCmd.Parameters.AddWithValue("@ProductID", productId);
                    object reasonObj = await fetchReasonCmd.ExecuteScalarAsync();
                    string dbReason = reasonObj?.ToString();
                    // Step 4: If reason qualifies, restock the item
                    // Normalize and trim before matching
                    string normalizedReason = dbReason?.Trim().ToLowerInvariant() ?? "";
                    string[] restockReasons = new[]
                    {
                    "received wrong item",
                    "product is defective or damaged",
                    "size-issue"
};
                    if (restockReasons.Contains(normalizedReason))
                    {
                        string restockQuery = "UPDATE Product SET Stock = Stock + 1 WHERE ProductID = @ProductID";
                        using SqlCommand restockCmd = new SqlCommand(restockQuery, conn);
                        restockCmd.Parameters.AddWithValue("@ProductID", productId);
                        await restockCmd.ExecuteNonQueryAsync();
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(":x: Error in SubmitReturnAsync: " + ex.Message);
                throw new Exception("Internal Server Error", ex);
            }
        }

        public async Task<List<ReturnRequestModel>> GetAllReturnsAsync()
        {
            var list = new List<ReturnRequestModel>();
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "SELECT * FROM ReturnRequest ORDER BY CreatedAt DESC";
            using SqlCommand cmd = new SqlCommand(query, conn);
            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReturnRequest(reader));
            }

            return list;
        }

        public async Task<List<ReturnRequestModel>> GetReturnsByOrderIdAsync(int orderId)
        {
            var list = new List<ReturnRequestModel>();
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "SELECT * FROM ReturnRequest WHERE OrderID = @OrderID ORDER BY CreatedAt DESC";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            await conn.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapReturnRequest(reader));
            }

            return list;
        }

        public async Task<ReturnRequestModel?> GetReturnByIdAsync(int returnId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ReturnRequest WHERE ReturnID = @ReturnID";
            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReturnID", returnId);

            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapReturnRequest(reader);
            }
            return null;
        }

        public async Task<bool> UpdateReturnStatusAsync(int returnId, string newStatus)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            string query = "UPDATE ReturnRequest SET Status = @Status WHERE ReturnID = @ReturnID";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@ReturnID", returnId);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<ProductModel>> GetProductsByOrderIdAsync(int orderId)
        {
            var products = new List<ProductModel>();

            var sql = @"
                SELECT p.ProductID, p.Name, p.Price, p.Stock, p.Description
                FROM OrderItem oi
                JOIN Product p ON oi.ProductID = p.ProductID
                WHERE oi.OrderID = @OrderID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderID", orderId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var productList = new List<ProductModel>();

            while (await reader.ReadAsync())
            {
                var product = new ProductModel
                {
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Description"))
                };

                productList.Add(product);
            }

            await reader.CloseAsync();

            // Load images for each product - loading relative paths
            foreach (var product in productList)
            {
                var imageCmd = new SqlCommand(@"
                    SELECT ImageData 
                    FROM ProductImage 
                    WHERE ProductID = @ProductID", conn);

                imageCmd.Parameters.AddWithValue("@ProductID", product.ProductID);

                using var imageReader = await imageCmd.ExecuteReaderAsync();

                var imagePaths = new List<string>();

                while (await imageReader.ReadAsync())
                {
                    if (!imageReader.IsDBNull(0))
                    {
                        string? imagePath = imageReader.GetString(0);
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            imagePaths.Add(imagePath);
                        }
                    }
                }

                await imageReader.CloseAsync();

                product.ImageUrls = imagePaths;

                products.Add(product);
            }

            return products;
        }

        private ReturnRequestModel MapReturnRequest(SqlDataReader reader)
        {
            string? rawImage = reader["ImageUrl"] as string;
            string? relativePath = null;

            if (!string.IsNullOrEmpty(rawImage))
            {
                // If already a relative URL path (starts with "/uploads/"), use directly
                if (rawImage.StartsWith("/uploads/"))
                {
                    relativePath = rawImage;
                }
                else
                {
                    // Otherwise, assume it's a physical file path and convert to relative URL
                    string fileName = Path.GetFileName(rawImage);
                    relativePath = "/uploads/returns/" + fileName;
                }
            }

            return new ReturnRequestModel
            {
                ReturnID = reader.GetInt32(reader.GetOrdinal("ReturnID")),
                OrderID = reader.GetInt32(reader.GetOrdinal("OrderID")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                ProductCondition = reader.GetString(reader.GetOrdinal("ProductCondition")),
                Comment = reader["Comment"] as string,
                ImageUrl = relativePath,
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
