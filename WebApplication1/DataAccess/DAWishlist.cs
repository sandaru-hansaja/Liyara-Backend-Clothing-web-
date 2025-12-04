using System.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.DataAccess
{
    //public class DAWishlist
    //{
    //    private readonly string _connectionString;

    //    public DAWishlist(IConfiguration configuration)
    //    {
    //        _connectionString = configuration.GetConnectionString("DefaultConnection");
    //    }

    //    public async Task<bool> AddToWishlistAsync(int customerId, int productId)
    //    {
    //        using var conn = new SqlConnection(_connectionString);
    //        using var cmd = new SqlCommand("INSERT INTO Wishlist (CustomerID, ProductID) VALUES (@CustomerID, @ProductID)", conn);
    //        cmd.Parameters.AddWithValue("@CustomerID", customerId);
    //        cmd.Parameters.AddWithValue("@ProductID", productId);

    //        await conn.OpenAsync();
    //        var rows = await cmd.ExecuteNonQueryAsync();
    //        return rows > 0;
    //    }

    //    public async Task<List<ProductDto>> GetWishlistByCustomerAsync(int customerId)
    //    {
    //        var wishlist = new List<ProductDto>();

    //        var sql = @"
    //        SELECT p.ProductID, p.Name, p.Price, p.Stock, p.Status
    //        FROM Wishlist w
    //        JOIN Product p ON w.ProductID = p.ProductID
    //        WHERE w.CustomerID = @CustomerID";

    //        using var conn = new SqlConnection(_connectionString);
    //        using var cmd = new SqlCommand(sql, conn);
    //        cmd.Parameters.AddWithValue("@CustomerID", customerId);

    //        await conn.OpenAsync();
    //        using var reader = await cmd.ExecuteReaderAsync();

    //        while (await reader.ReadAsync())
    //        {
    //            wishlist.Add(new ProductDto
    //            {
    //                ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
    //                Name = reader.GetString(reader.GetOrdinal("Name")),
    //                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
    //                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),

    //            });
    //        }

    //        return wishlist;
    //    }
    public class DAWishlist
    {
        private readonly string _connectionString;

        public DAWishlist(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<bool> AddToWishlistAsync(int customerId, int productId)
        {
            // Optional: Check if already exists
            if (await IsProductInWishlist(customerId, productId))
                return true;

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("INSERT INTO Wishlist (CustomerID, ProductID) VALUES (@CustomerID, @ProductID)", conn);
            cmd.Parameters.AddWithValue("@CustomerID", customerId);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private async Task<bool> IsProductInWishlist(int customerId, int productId)
        {
            var sql = "SELECT COUNT(*) FROM Wishlist WHERE CustomerID = @CustomerID AND ProductID = @ProductID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CustomerID", customerId);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            await conn.OpenAsync();
            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<List<ProductModel>> GetWishlistByCustomerAsync(int customerId)
        {
            var wishlist = new List<ProductModel>();

            var sql = @"
        SELECT 
            p.ProductID,
            p.Name,
            p.Price,
            p.Stock,
            p.Description,
            c.CategoryName,
            s.SubCategoryName
        FROM Wishlist w
        JOIN Product p ON w.ProductID = p.ProductID
        LEFT JOIN Category c ON p.CategoryID = c.CategoryID
        LEFT JOIN SubCategory s ON p.SubCategoryID = s.SubCategoryID
        WHERE w.CustomerID = @CustomerID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CustomerID", customerId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int productId = reader.GetInt32(reader.GetOrdinal("ProductID"));

                var product = new ProductModel
                {
                    ProductID = productId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                    SubCategoryName = reader.IsDBNull(reader.GetOrdinal("SubCategoryName")) ? null : reader.GetString(reader.GetOrdinal("SubCategoryName")),
                    ImageUrls = await GetProductImagesAsync(productId),
                    Colors = await GetProductColorsAsync(productId),
                    Sizes = await GetProductSizesAsync(productId)
                };

                wishlist.Add(product);
            }

            return wishlist;
        }


        //private async Task<List<string>> GetProductImagesAsync(int productId)
        //{
        //    var result = new List<string>();
        //    var sql = "SELECT ImageData FROM ProductImage WHERE ProductID = @ProductID";

        //    using var conn = new SqlConnection(_connectionString);
        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.AddWithValue("@ProductID", productId);
        //    await conn.OpenAsync();

        //    using var reader = await cmd.ExecuteReaderAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        if (!reader.IsDBNull(reader.GetOrdinal("ImageData")))
        //        {
        //            string relativePath = reader["ImageData"].ToString();
        //            string fullUrl = $"https://liarafashion.dockyardsoftware.com/uploads/products/{relativePath}";
        //            result.Add(fullUrl);
        //        }
        //    }

        //    return result;
        //}
        private async Task<List<string>> GetProductImagesAsync(int productId)
        {
            var result = new List<string>();
            var sql = "SELECT ImageData FROM ProductImage WHERE ProductID = @ProductID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? rawImage = reader["ImageData"] as string;
                string? relativePath = null;

                if (!string.IsNullOrEmpty(rawImage))
                {
                    // If already starts with "/uploads/", assume it's already a relative path
                    if (rawImage.StartsWith("/uploads/"))
                    {
                        relativePath = rawImage;
                    }
                    else
                    {
                        // Otherwise, extract the filename and build relative path
                        string fileName = Path.GetFileName(rawImage);
                        relativePath = "/uploads/products/" + fileName;
                    }

                    result.Add(relativePath);
                }
            }

            return result;
        }




        private async Task<List<string>> GetProductColorsAsync(int productId)
        {
            var result = new List<string>();
            var sql = @"
        SELECT c.ColorName
        FROM ProductColor pc
        JOIN Color c ON pc.ColorID = c.ColorID
        WHERE pc.ProductID = @ProductID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(reader.GetOrdinal("ColorName")));
            }

            return result;
        }


        private async Task<List<string>> GetProductSizesAsync(int productId)
        {
            var result = new List<string>();
            var sql = @"
        SELECT s.SizeName
        FROM ProductSize ps
        JOIN Size s ON ps.SizeID = s.SizeID
        WHERE ps.ProductID = @ProductID";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(reader.GetOrdinal("SizeName")));
            }

            return result;
        }


        public async Task<bool> RemoveFromWishlistAsync(int customerId, int productId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM Wishlist WHERE CustomerID = @CustomerID AND ProductID = @ProductID", conn);
            cmd.Parameters.AddWithValue("@CustomerID", customerId);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}

