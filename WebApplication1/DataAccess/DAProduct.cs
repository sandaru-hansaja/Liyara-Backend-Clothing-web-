using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Controllers;
using WebApplication1.Models;

namespace WebApplication1.DataAccess
{
    public class DAProduct
    {
        private readonly string _connectionString;

        public DAProduct(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        // Helper: convert image byte[] to Base64 string
        private string? ConvertImageToBase64(byte[]? imageData)
        {
            return imageData != null ? Convert.ToBase64String(imageData) : null;
        }

        // Search products by keyword
        public async Task<List<object>> SearchProductsAsync(string? keyword)
        {
            var results = new List<object>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
               SELECT p.ProductID, p.Name, p.CategoryID, p.SubCategoryID, p.Stock, p.Price, p.Slug,p.Description,
               c.CategoryName, sc.SubCategoryName,
               pi.ImageID, pi.ImageData,
               pc.ColorID, col.ColorName,
               ps.SizeID, sz.SizeName
                FROM Product p
                LEFT JOIN Category c ON p.CategoryID = c.CategoryID
                LEFT JOIN SubCategory sc ON p.SubCategoryID = sc.SubCategoryID
                LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductId
                LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
                LEFT JOIN Color col ON pc.ColorID = col.ColorID
                LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
                LEFT JOIN Size sz ON ps.SizeID = sz.SizeID
                WHERE (@keyword IS NULL OR
               LOWER(p.Name) LIKE '%' + LOWER(@keyword) + '%' OR
               LOWER(c.CategoryName) LIKE '%' + LOWER(@keyword) + '%' OR
               LOWER(sc.SubCategoryName) LIKE '%' + LOWER(@keyword) + '%')";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@keyword", (object?)keyword ?? DBNull.Value);

            var reader = await cmd.ExecuteReaderAsync();

            var productDict = new Dictionary<int, dynamic>();

            while (await reader.ReadAsync())
            {
                int productId = (int)reader["ProductID"];
                if (!productDict.ContainsKey(productId))
                {
                    productDict[productId] = new
                    {
                        ProductID = productId,
                        Name = reader["Name"] as string ?? "No Name Available",
                        CategoryID = reader["CategoryID"] as int? ?? 0,
                        Description = reader["Description"] as string,
                        CategoryName = reader["CategoryName"] as string ?? "Uncategorized",
                        SubCategoryID = reader["SubCategoryID"] as int? ?? 0,
                        SubCategoryName = reader["SubCategoryName"] as string ?? "No SubCategory",
                        Stock = reader["Stock"] as int? ?? 0,
                        Status = ((int)reader["Stock"] == 0) ? "Sold Out" : "Available",
                        Price = reader["Price"] as decimal? ?? 0,
                        Colors = new List<dynamic>(),
                        Sizes = new List<dynamic>(),
                        Images = new List<dynamic>(),
                        Slug = reader["Slug"] as string
                    };
                }

                var product = productDict[productId];

                if (reader["ColorID"] != DBNull.Value)
                {
                    int colorId = (int)reader["ColorID"];
                    string colorName = (string)reader["ColorName"];
                    var colors = (List<dynamic>)product.Colors;
                    if (!colors.Any(c => c.ColorID == colorId))
                        colors.Add(new { ColorID = colorId, Name = colorName });
                }

                if (reader["SizeID"] != DBNull.Value)
                {
                    int sizeId = (int)reader["SizeID"];
                    string sizeName = (string)reader["SizeName"];
                    var sizes = (List<dynamic>)product.Sizes;
                    if (!sizes.Any(s => s.SizeID == sizeId))
                        sizes.Add(new { SizeID = sizeId, Name = sizeName });
                }


                if (reader["ImageID"] != DBNull.Value)
                {
                    int imageId = reader.GetInt32(reader.GetOrdinal("ImageID"));
                    string? imagePath = reader["ImageData"] as string; // ← read path as string
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        var images = (List<dynamic>)product.Images;
                        if (!images.Any(i => (int)i.ImageID == imageId))
                            images.Add(new { ImageID = imageId, Path = imagePath });
                    }
                }
            }
            reader.Close();

            results = productDict.Values.Cast<object>().ToList();
            return results;
        }

        public async Task<List<object>> GetProductsAsync(int? categoryId, int[]? colorIds, int[]? sizeIds)
        {
            var results = new List<object>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
        SELECT p.ProductID, p.Name, p.CategoryID, p.SubCategoryID, p.Stock, p.Price,p.Description,
               c.CategoryName,
               pi.ImageID, pi.ImageData,
               pc.ColorID,
               ps.SizeID
                FROM Product p
                LEFT JOIN Category c ON p.CategoryID = c.CategoryID
                LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductId
                LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
                LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
                WHERE (@categoryId IS NULL OR p.CategoryID = @categoryId)";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@categoryId", (object?)categoryId ?? DBNull.Value);

            var reader = await cmd.ExecuteReaderAsync();

            var productDict = new Dictionary<int, dynamic>();
            while (await reader.ReadAsync())
            {
                int productId = (int)reader["ProductID"];
                if (!productDict.ContainsKey(productId))
                {
                    productDict[productId] = new
                    {
                        ProductID = productId,
                        Name = reader["Name"] as string,
                        CategoryID = reader["CategoryID"] as int? ?? 0,
                        Description = reader["Description"] as string,
                        CategoryName = reader["CategoryName"] as string ?? "Unknown",
                        SubCategoryID = reader["SubCategoryID"] as int? ?? 0,
                        Stock = reader["Stock"] as int? ?? 0,
                        Price = reader["Price"] as decimal? ?? 0,
                        Colors = new List<int>(),
                        Sizes = new List<int>(),
                        Images = new List<dynamic>()
                    };
                }

                var product = productDict[productId];

                if (reader["ColorID"] != DBNull.Value)
                {
                    int colorId = (int)reader["ColorID"];
                    var colors = (List<int>)product.Colors;
                    if (!colors.Contains(colorId))
                        colors.Add(colorId);
                }

                if (reader["SizeID"] != DBNull.Value)
                {
                    int sizeId = (int)reader["SizeID"];
                    var sizes = (List<int>)product.Sizes;
                    if (!sizes.Contains(sizeId))
                        sizes.Add(sizeId);
                }


                if (reader["ImageID"] != DBNull.Value)
                {
                    int imageId = reader.GetInt32(reader.GetOrdinal("ImageID"));
                    string? imagePath = reader["ImageData"] as string; // ← read path as string
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        var images = (List<dynamic>)product.Images;
                        if (!images.Any(i => (int)i.ImageID == imageId))
                            images.Add(new { ImageID = imageId, Path = imagePath });
                    }
                }
            }
            reader.Close();

            // Apply colorIds filter
            if (colorIds != null && colorIds.Length > 0)
            {
                results = productDict.Values
                    .Where(p => ((List<int>)p.Colors).Any(c => colorIds.Contains(c)))
                    .ToList<object>();
            }
            else
            {
                results = productDict.Values.ToList<object>();
            }

            // Apply sizeIds filter
            if (sizeIds != null && sizeIds.Length > 0)
            {
                results = results
                    .Where(p => ((List<int>)((dynamic)p).Sizes).Any(s => sizeIds.Contains(s)))
                    .ToList();
            }

            return results;
        }

        public async Task<List<object>> GetAllProductsAsync()
        {
            return await SearchProductsAsync(null);
        }

        public async Task<List<object>> GetLatestProductsAsync(int count = 8)
        {
            var results = new List<object>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = $@"
        SELECT TOP {count} 
            p.ProductID, p.Name, p.CategoryID, p.SubCategoryID, p.Stock, p.Price,p.Description,
            c.CategoryName,
            pi.ImageID, pi.ImageData, -- ← still the column name
            pc.ColorID, col.ColorName,
            ps.SizeID, sz.SizeName
        FROM Product p
        LEFT JOIN Category c ON p.CategoryID = c.CategoryID
        LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductId
        LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
        LEFT JOIN Color col ON pc.ColorID = col.ColorID
        LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
        LEFT JOIN Size sz ON ps.SizeID = sz.SizeID
        ORDER BY p.ProductID DESC";

            using var cmd = new SqlCommand(sql, conn);
            var reader = await cmd.ExecuteReaderAsync();

            var productDict = new Dictionary<int, dynamic>();

            while (await reader.ReadAsync())
            {
                int productId = reader.GetInt32(reader.GetOrdinal("ProductID"));
                if (!productDict.ContainsKey(productId))
                {
                    productDict[productId] = new
                    {
                        ProductID = productId,
                        Name = reader["Name"] as string ?? "No Name Available",
                        CategoryID = reader["CategoryID"] as int? ?? 0,
                        Description = reader["Description"] as string,
                        CategoryName = reader["CategoryName"] as string ?? "Uncategorized",
                        SubCategoryID = reader["SubCategoryID"] as int? ?? 0,
                        Stock = reader["Stock"] as int? ?? 0,
                        Price = reader["Price"] as decimal? ?? 0,
                        Colors = new List<dynamic>(),
                        Sizes = new List<dynamic>(),
                        Images = new List<dynamic>()
                    };
                }

                var product = productDict[productId];

                if (reader["ColorID"] != DBNull.Value)
                {
                    int colorId = reader.GetInt32(reader.GetOrdinal("ColorID"));
                    string colorName = reader.GetString(reader.GetOrdinal("ColorName"));
                    var colors = (List<dynamic>)product.Colors;
                    if (!colors.Any(c => (int)c.ColorID == colorId))
                        colors.Add(new { ColorID = colorId, Name = colorName });
                }

                if (reader["SizeID"] != DBNull.Value)
                {
                    int sizeId = reader.GetInt32(reader.GetOrdinal("SizeID"));
                    string sizeName = reader.GetString(reader.GetOrdinal("SizeName"));
                    var sizes = (List<dynamic>)product.Sizes;
                    if (!sizes.Any(s => (int)s.SizeID == sizeId))
                        sizes.Add(new { SizeID = sizeId, Name = sizeName });
                }

                if (reader["ImageID"] != DBNull.Value)
                {
                    int imageId = reader.GetInt32(reader.GetOrdinal("ImageID"));
                    string? imagePath = reader["ImageData"] as string; // ← read path as string
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        var images = (List<dynamic>)product.Images;
                        if (!images.Any(i => (int)i.ImageID == imageId))
                            images.Add(new { ImageID = imageId, Path = imagePath });
                    }
                }
            }

            reader.Close();
            results = productDict.Values.Cast<object>().ToList();
            return results;
        }

        //public async Task<object?> GetProductDetailsAsync(int productId)
        //{
        //    try
        //    {
        //        using var conn = new SqlConnection(_connectionString);
        //        await conn.OpenAsync();

        //        var sql = @"
        //    SELECT 
        //        p.ProductID, p.Name, p.Price, p.Status,p.Stock,
        //        c.CategoryName,
        //        sc.SubCategoryName, 
        //        col.ColorName,
        //        s.SizeName,
        //        pi.ImageData
        //    FROM Product p
        //    LEFT JOIN Category c ON p.CategoryID = c.CategoryID
        //    LEFT JOIN SubCategory sc ON p.SubCategoryID = sc.SubCategoryID
        //    LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
        //    LEFT JOIN Color col ON pc.ColorID = col.ColorID
        //    LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
        //    LEFT JOIN Size s ON ps.SizeID = s.SizeID
        //    LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductID
        //    WHERE p.ProductID = @id";

        //        using var cmd = new SqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@id", productId);

        //        var reader = await cmd.ExecuteReaderAsync();

        //        object? product = null;
        //        var colors = new HashSet<string>();
        //        var sizes = new HashSet<string>();
        //        var images = new HashSet<string>();

        //        while (await reader.ReadAsync())
        //        {
        //            if (product == null)
        //            {
        //                product = new
        //                {
        //                    ProductID = reader["ProductID"],
        //                    Name = reader["Name"] as string,
        //                    CategoryName = reader["CategoryName"] as string,
        //                    SubCategoryName = reader["SubCategoryName"] as string,
        //                    Stock = reader["Stock"] as int?,
        //                    Price = reader["Price"] as decimal?,
        //                    Status = reader["Status"] as string,
        //                    Colors = colors,
        //                    Sizes = sizes,
        //                    Images = images
        //                };
        //            }

        //            if (reader["ColorName"] != DBNull.Value)
        //                colors.Add(reader["ColorName"].ToString()!);

        //            if (reader["SizeName"] != DBNull.Value)
        //                sizes.Add(reader["SizeName"].ToString()!);

        //            if (reader["ImageData"] != DBNull.Value)
        //            {
        //                byte[]? imgData = reader["ImageData"] as byte[];
        //                if (imgData != null)
        //                {
        //                    string base64 = Convert.ToBase64String(imgData);
        //                    images.Add(base64);
        //                }
        //            }
        //        }

        //        if (product != null)
        //        {
        //            return new
        //            {
        //                ((dynamic)product).ProductID,
        //                ((dynamic)product).Name,
        //                ((dynamic)product).CategoryName,
        //                ((dynamic)product).SubCategoryName,
        //                ((dynamic)product).Stock,
        //                ((dynamic)product).Price,
        //                ((dynamic)product).Status,
        //                Colors = new List<string>(((dynamic)product).Colors),
        //                Sizes = new List<string>(((dynamic)product).Sizes),
        //                Images = new List<string>(((dynamic)product).Images)
        //            };
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("💥 ERROR in GetProductDetailsAsync:");
        //        Console.WriteLine("Message: " + ex.Message);
        //        Console.WriteLine("Stack Trace: " + ex.StackTrace);
        //        throw;
        //    }
        //}
        public async Task<object?> GetProductDetailsAsync(int productId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
            SELECT 
                p.ProductID, p.Name, p.Price, p.Status, p.Stock,  p.Description, 
                c.CategoryName,
                sc.SubCategoryName, 
                col.ColorName,
                s.SizeName,
                pi.ImageData
            FROM Product p
            LEFT JOIN Category c ON p.CategoryID = c.CategoryID
            LEFT JOIN SubCategory sc ON p.SubCategoryID = sc.SubCategoryID
            LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
            LEFT JOIN Color col ON pc.ColorID = col.ColorID
            LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
            LEFT JOIN Size s ON ps.SizeID = s.SizeID
            LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductID
            WHERE p.ProductID = @id";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", productId);

                var reader = await cmd.ExecuteReaderAsync();

                object? product = null;
                var colors = new HashSet<string>();
                var sizes = new HashSet<string>();
                var images = new HashSet<string>();

                while (await reader.ReadAsync())
                {
                    if (product == null)
                    {
                        product = new
                        {
                            ProductID = reader["ProductID"],
                            Name = reader["Name"] as string,
                            CategoryName = reader["CategoryName"] as string,
                            Description = reader["Description"] as string,
                            SubCategoryName = reader["SubCategoryName"] as string,
                            Stock = reader["Stock"] as int?,
                            Price = reader["Price"] as decimal?,
                            Status = reader["Status"] as string,
                            Colors = colors,
                            Sizes = sizes,
                            Images = images
                        };
                    }

                    if (reader["ColorName"] != DBNull.Value)
                        colors.Add(reader["ColorName"].ToString()!);

                    if (reader["SizeName"] != DBNull.Value)
                        sizes.Add(reader["SizeName"].ToString()!);

                    if (reader["ImageData"] != DBNull.Value)
                    {
                        string imagePath = reader["ImageData"].ToString()!;
                        if (!string.IsNullOrEmpty(imagePath))
                            images.Add(imagePath);
                    }
                }

                if (product != null)
                {
                    return new
                    {
                        ((dynamic)product).ProductID,
                        ((dynamic)product).Name,
                        ((dynamic)product).CategoryName,
                        ((dynamic)product).Description,
                        ((dynamic)product).SubCategoryName,
                        ((dynamic)product).Stock,
                        ((dynamic)product).Price,
                        ((dynamic)product).Status,
                        Colors = new List<string>(((dynamic)product).Colors),
                        Sizes = new List<string>(((dynamic)product).Sizes),
                        Images = new List<string>(((dynamic)product).Images)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 ERROR in GetProductDetailsAsync:");
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
                throw;
            }
        }


        public async Task<bool> DeleteProductAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                var deleteImagesSql = "DELETE FROM ProductImage WHERE ProductId = @id";
                using var deleteImagesCmd = new SqlCommand(deleteImagesSql, conn, transaction);
                deleteImagesCmd.Parameters.AddWithValue("@id", id);
                await deleteImagesCmd.ExecuteNonQueryAsync();

                var deleteProductSql = "DELETE FROM Product WHERE ProductID = @id";
                using var deleteProductCmd = new SqlCommand(deleteProductSql, conn, transaction);
                deleteProductCmd.Parameters.AddWithValue("@id", id);
                var affected = await deleteProductCmd.ExecuteNonQueryAsync();

                transaction.Commit();

                return affected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> AddProductAsync(ProductModel dto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                int? categoryId = await GetCategoryIdByNameAsync(dto.CategoryName, conn, transaction);
                if (categoryId == null)
                    throw new Exception("Invalid Category Name.");

                int? subCategoryId = await GetSubCategoryIdByNameAndCategoryAsync(dto.SubCategoryName, categoryId.Value, conn, transaction);
                if (subCategoryId == null)
                    throw new Exception("Invalid SubCategory Name for the given Category.");

                var colorIds = await GetColorIdsByNamesAsync(dto.Colors, conn, transaction);
                if (colorIds.Count != dto.Colors.Count)
                    throw new Exception("One or more selected colors do not exist in the database.");

                var sizeIds = await GetSizeIdsByNamesAsync(dto.Sizes, conn, transaction);
                if (sizeIds.Count != dto.Sizes.Count)
                    throw new Exception("One or more selected sizes do not exist in the database.");

                string slug = dto.Name.Replace(" ", "-").ToLower();

                string insertProduct = @"
        INSERT INTO Product (Name, CategoryID, SubCategoryID, Stock, Price, Status, Slug, Description)
        OUTPUT INSERTED.ProductID
        VALUES (@Name, @CategoryID, @SubCategoryID, @Stock, @Price, @Status, @Slug, @Description)";

                using var insertCmd = new SqlCommand(insertProduct, conn, transaction);
                insertCmd.Parameters.AddWithValue("@Name", dto.Name);
                insertCmd.Parameters.AddWithValue("@CategoryID", categoryId.Value);
                insertCmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId.Value);
                insertCmd.Parameters.AddWithValue("@Stock", dto.Stock);
                insertCmd.Parameters.AddWithValue("@Price", dto.Price);
                insertCmd.Parameters.AddWithValue("@Status", dto.Stock > 0 ? "Available" : "Sold Out");
                insertCmd.Parameters.AddWithValue("@Slug", slug);
                insertCmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);

                int productId = (int)await insertCmd.ExecuteScalarAsync();

                foreach (var colorId in colorIds)
                {
                    string insertColor = "INSERT INTO ProductColor (ProductID, ColorID) VALUES (@ProductID, @ColorID)";
                    using var colorCmd = new SqlCommand(insertColor, conn, transaction);
                    colorCmd.Parameters.AddWithValue("@ProductID", productId);
                    colorCmd.Parameters.AddWithValue("@ColorID", colorId);
                    await colorCmd.ExecuteNonQueryAsync();
                }

                foreach (var sizeId in sizeIds)
                {
                    string insertSize = "INSERT INTO ProductSize (ProductID, SizeID) VALUES (@ProductID, @SizeID)";
                    using var sizeCmd = new SqlCommand(insertSize, conn, transaction);
                    sizeCmd.Parameters.AddWithValue("@ProductID", productId);
                    sizeCmd.Parameters.AddWithValue("@SizeID", sizeId);
                    await sizeCmd.ExecuteNonQueryAsync();
                }

                // ✅ Use consistent and correct physical path for saving images
                var uploadsFolder = @"C:\inetpub\wwwroot\liarabackend\wwwroot\uploads\products";
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var image in dto.Images)
                {
                    // Generate unique file name
                    var uniqueFileName = $"product_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save image file to disk
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await image.CopyToAsync(fileStream);

                    // ✅ Store relative URL path for frontend
                    string relativePath = $"/uploads/products/{uniqueFileName}";

                    string insertImage = "INSERT INTO ProductImage (ProductId, ImageData) VALUES (@ProductID, @ImageData)";
                    using var imageCmd = new SqlCommand(insertImage, conn, transaction);
                    imageCmd.Parameters.AddWithValue("@ProductID", productId);
                    imageCmd.Parameters.AddWithValue("@ImageData", relativePath);
                    await imageCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }



        private async Task<int?> GetCategoryIdByNameAsync(string categoryName, SqlConnection conn, SqlTransaction tran)
        {
            var sql = "SELECT CategoryID FROM Category WHERE LOWER(CategoryName) = LOWER(@name)";
            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.AddWithValue("@name", categoryName);
            var result = await cmd.ExecuteScalarAsync();
            return result as int?;
        }

        private async Task<int?> GetSubCategoryIdByNameAndCategoryAsync(string subCategoryName, int categoryId, SqlConnection conn, SqlTransaction tran)
        {
            var sql = @"
                SELECT SubCategoryID FROM SubCategory
                WHERE LOWER(SubCategoryName) = LOWER(@subName) AND CategoryID = @catId";
            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.AddWithValue("@subName", subCategoryName);
            cmd.Parameters.AddWithValue("@catId", categoryId);
            var result = await cmd.ExecuteScalarAsync();
            return result as int?;
        }

        private async Task<List<int>> GetColorIdsByNamesAsync(List<string> colors, SqlConnection conn, SqlTransaction tran)
        {
            var result = new List<int>();
            var colorsParam = string.Join(",", colors.Select((c, i) => $"@color{i}"));
            var sql = $"SELECT ColorID, ColorName FROM Color WHERE LOWER(ColorName) IN ({colorsParam})";

            using var cmd = new SqlCommand(sql, conn, tran);
            for (int i = 0; i < colors.Count; i++)
                cmd.Parameters.AddWithValue($"@color{i}", colors[i].ToLower());

            using var reader = await cmd.ExecuteReaderAsync();
            var dbColors = new Dictionary<string, int>();
            while (await reader.ReadAsync())
            {
                string name = ((string)reader["ColorName"]).ToLower();
                int id = (int)reader["ColorID"];
                dbColors[name] = id;
            }
            reader.Close();

            foreach (var color in colors)
            {
                if (dbColors.TryGetValue(color.ToLower(), out var id))
                    result.Add(id);
            }

            return result;
        }

        public async Task<bool> UpdateProductAsync(UpdateProductModel dto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                var colorIds = new List<int>();
                if (dto.Colors != null && dto.Colors.Any())
                {
                    colorIds = await GetColorIdsByNamesAsync(dto.Colors, conn, transaction);
                    if (colorIds.Count != dto.Colors.Count)
                        throw new Exception("One or more selected colors do not exist in the database.");
                }

                var sizeIds = new List<int>();
                if (dto.Sizes != null && dto.Sizes.Any())
                {
                    sizeIds = await GetSizeIdsByNamesAsync(dto.Sizes, conn, transaction);
                    if (sizeIds.Count != dto.Sizes.Count)
                        throw new Exception("One or more selected sizes do not exist in the database.");
                }

                    string updateProductSql = @"
                        UPDATE Product SET 
                            Name = @Name,
                            Stock = @Stock,
                            Price = @Price,
                            Status = @Status,
                            Description = @Description
                        WHERE ProductID = @ProductID";

                using var updateCmd = new SqlCommand(updateProductSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@Name", dto.Name);
                updateCmd.Parameters.AddWithValue("@Stock", dto.Stock);
                updateCmd.Parameters.AddWithValue("@Price", dto.Price);
                updateCmd.Parameters.AddWithValue("@Status", dto.Stock > 0 ? "Available" : "Sold Out");
                updateCmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);
                updateCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                    throw new Exception("Product not found.");

                using var delColorsCmd = new SqlCommand("DELETE FROM ProductColor WHERE ProductID = @ProductID", conn, transaction);
                delColorsCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                await delColorsCmd.ExecuteNonQueryAsync();

                if (colorIds.Any())
                {
                    foreach (var colorId in colorIds)
                    {
                        using var insColorCmd = new SqlCommand("INSERT INTO ProductColor (ProductID, ColorID) VALUES (@ProductID, @ColorID)", conn, transaction);
                        insColorCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                        insColorCmd.Parameters.AddWithValue("@ColorID", colorId);
                        await insColorCmd.ExecuteNonQueryAsync();
                    }
                }

                using var delSizesCmd = new SqlCommand("DELETE FROM ProductSize WHERE ProductID = @ProductID", conn, transaction);
                delSizesCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                await delSizesCmd.ExecuteNonQueryAsync();

                if (sizeIds.Any())
                {
                    foreach (var sizeId in sizeIds)
                    {
                        using var insSizeCmd = new SqlCommand("INSERT INTO ProductSize (ProductID, SizeID) VALUES (@ProductID, @SizeID)", conn, transaction);
                        insSizeCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                        insSizeCmd.Parameters.AddWithValue("@SizeID", sizeId);
                        await insSizeCmd.ExecuteNonQueryAsync();
                    }
                }

                if (dto.Images?.Any() == true)
                {
                    using var delImagesCmd = new SqlCommand("DELETE FROM ProductImage WHERE ProductId = @ProductID", conn, transaction);
                    delImagesCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                    await delImagesCmd.ExecuteNonQueryAsync();

                    // ✅ Physical folder path to save images
                    var uploadsFolder = @"C:\inetpub\wwwroot\liarabackend\wwwroot\uploads\products";
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var image in dto.Images)
                    {
                        var uniqueFileName = $"product_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using var fileStream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(fileStream);

                        // ✅ Relative path stored in DB for frontend to access
                        string relativePath = $"/uploads/products/{uniqueFileName}";

                        string insertImage = "INSERT INTO ProductImage (ProductId, ImageData) VALUES (@ProductID, @ImageData)";
                        using var insImageCmd = new SqlCommand(insertImage, conn, transaction);
                        insImageCmd.Parameters.AddWithValue("@ProductID", dto.ProductID);
                        insImageCmd.Parameters.AddWithValue("@ImageData", relativePath);
                        await insImageCmd.ExecuteNonQueryAsync();
                    }
                }

            

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public async Task<bool> AddProductReviewAsync(ProductReviewModel review)
        {
            using var conn = new SqlConnection(_connectionString);
            var sql = @"
            INSERT INTO ProductReview (ProductID, UserName, ReviewText, ReviewDate, Rating, Likes, Dislikes)
            VALUES (@ProductID, @UserName, @ReviewText, @ReviewDate, @Rating, 0, 0)";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", review.ProductID);
            cmd.Parameters.AddWithValue("@UserName", review.UserName);
            cmd.Parameters.AddWithValue("@ReviewText", review.ReviewText);
            cmd.Parameters.AddWithValue("@ReviewDate", review.ReviewDate);
            cmd.Parameters.AddWithValue("@Rating", review.Rating);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<ProductReviewModel>> GetReviewsByProductIdAsync(int productId)
        {
            var reviews = new List<ProductReviewModel>();
            using var conn = new SqlConnection(_connectionString);
            var query = "SELECT * FROM ProductReview WHERE ProductID = @ProductID ORDER BY ReviewDate DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reviews.Add(new ProductReviewModel
                {
                    ReviewID = (int)reader["ReviewID"],
                    ProductID = (int)reader["ProductID"],
                    UserName = reader["UserName"].ToString(),
                    ReviewText = reader["ReviewText"].ToString(),
                    ReviewDate = (DateTime)reader["ReviewDate"],
                    Likes = (int)reader["Likes"],
                    Dislikes = (int)reader["Dislikes"],
                    Rating = (int)reader["Rating"],
                });
            }

            return reviews;
        }

        public async Task<bool> UpdateReviewReactionAsync(int reviewId, bool isLike)
        {
            using var conn = new SqlConnection(_connectionString);
            string column = isLike ? "Likes" : "Dislikes";
            string query = $"UPDATE ProductReview SET {column} = {column} + 1 WHERE ReviewID = @ReviewID";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ReviewID", reviewId);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<string>> GetAllCategoryNamesAsync()
        {
            var categories = new List<string>();

            using var conn = new SqlConnection(_connectionString);
            var sql = "SELECT CategoryName FROM Category";
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(reader.GetString(0));
            }

            return categories;
        }

        public async Task<List<string>> GetSubCategoryNamesByCategoryAsync(string categoryName)
        {
            var subCategories = new List<string>();

            using var conn = new SqlConnection(_connectionString);
            var sql = @"
        SELECT sc.SubCategoryName
        FROM SubCategory sc
        JOIN Category c ON sc.CategoryID = c.CategoryID
        WHERE c.CategoryName = @CategoryName";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                subCategories.Add(reader.GetString(0));
            }

            return subCategories;
        }

        private async Task<List<int>> GetSizeIdsByNamesAsync(List<string> sizes, SqlConnection conn, SqlTransaction tran)
        {
            var result = new List<int>();
            var sizesParam = string.Join(",", sizes.Select((s, i) => $"@size{i}"));
            var sql = $"SELECT SizeID, SizeName FROM Size WHERE LOWER(SizeName) IN ({sizesParam})";

            using var cmd = new SqlCommand(sql, conn, tran);
            for (int i = 0; i < sizes.Count; i++)
                cmd.Parameters.AddWithValue($"@size{i}", sizes[i].ToLower());

            using var reader = await cmd.ExecuteReaderAsync();
            var dbSizes = new Dictionary<string, int>();
            while (await reader.ReadAsync())
            {
                string name = ((string)reader["SizeName"]).ToLower();
                int id = (int)reader["SizeID"];
                dbSizes[name] = id;
            }
            reader.Close();

            foreach (var size in sizes)
            {
                if (dbSizes.TryGetValue(size.ToLower(), out var id))
                    result.Add(id);
            }

            return result;
        }

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
                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
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
    }
}