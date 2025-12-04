using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.DataAccess
{
    public class DACategory
    {
        private readonly string _connectionString;

       

        public DACategory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }



        // Get all categories with subcategories
        public async Task<List<object>> GetCategoriesAsync()
        {
            var categories = new List<object>();

            string query = @"
        SELECT 
            c.CategoryID, 
            c.CategoryName, 
            c.States AS CategoryStates, 
            sc.SubCategoryID, 
            sc.SubCategoryName,
            sc.States AS SubCategoryStates
        FROM Category c
        LEFT JOIN SubCategory sc ON c.CategoryID = sc.CategoryID
        -- Uncomment the line below if you want to filter only active categories and subcategories
        -- WHERE c.States = 'A' AND (sc.States = 'A' OR sc.States IS NULL)
        ORDER BY c.CategoryID, sc.SubCategoryID";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            int? currentCategoryId = null;
            string currentCategoryName = null;
            string currentCategoryStates = null;
            List<object> currentSubCategories = null;

            while (await reader.ReadAsync())
            {
                int catId = reader.GetInt32(0);
                string catName = reader.GetString(1);
                string catStates = reader.GetString(2); // Category states
                int? subCatId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                string subCatName = reader.IsDBNull(4) ? null : reader.GetString(4);
                string subCatStates = reader.IsDBNull(5) ? null : reader.GetString(5); // SubCategory states

                if (currentCategoryId != catId)
                {
                    if (currentCategoryId != null)
                    {
                        categories.Add(new
                        {
                            id = currentCategoryId,
                            name = currentCategoryName,
                            states = currentCategoryStates,
                            subCategories = currentSubCategories
                        });
                    }

                    currentCategoryId = catId;
                    currentCategoryName = catName;
                    currentCategoryStates = catStates;
                    currentSubCategories = new List<object>();
                }

                if (subCatId != null)
                {
                    currentSubCategories.Add(new
                    {
                        id = subCatId.Value,
                        name = subCatName,
                        states = subCatStates
                    });
                }
            }

            // Add last category
            if (currentCategoryId != null)
            {
                categories.Add(new
                {
                    id = currentCategoryId,
                    name = currentCategoryName,
                    states = currentCategoryStates,
                    subCategories = currentSubCategories
                });
            }

            return categories;
        }


        // Check if category exists by name
        public async Task<bool> CategoryExistsAsync(string categoryName)
        {
            string query = "SELECT COUNT(1) FROM Category WHERE CategoryName = @CategoryName";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);

            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        // Add category with subcategories
        public async Task<CategoryModel> AddCategoryWithSubcategoriesAsync(CategoryModel category)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Insert category
                string insertCategoryQuery = @"
                    INSERT INTO Category (CategoryName) VALUES (@CategoryName);
                    SELECT CAST(scope_identity() AS int);
                ";

                using var cmd = new SqlCommand(insertCategoryQuery, conn, transaction);
                cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName);

                int newCategoryId = (int)await cmd.ExecuteScalarAsync();

                // Insert subcategories
                if (category.SubCategory != null && category.SubCategory.Count > 0)
                {
                    string insertSubCategoryQuery = @"
                        INSERT INTO SubCategory (SubCategoryName, CategoryID)
                        VALUES (@SubCategoryName, @CategoryID);
                    ";

                    foreach (var sc in category.SubCategory)
                    {
                        using var subCmd = new SqlCommand(insertSubCategoryQuery, conn, transaction);
                        subCmd.Parameters.AddWithValue("@SubCategoryName", sc.SubCategoryName ?? "");
                        subCmd.Parameters.AddWithValue("@CategoryID", newCategoryId);
                        await subCmd.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();

                // Return the category with new ID
                category.CategoryID = newCategoryId;
                return category;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        //public async Task<bool> UpdateSubCategoriesAsync(string categoryName, List<string> updatedSubCategoryNames)
        //{
        //    using var conn = new SqlConnection(_connectionString);
        //    await conn.OpenAsync();
        //    using var transaction = conn.BeginTransaction();

        //    try
        //    {
        //        // 1. Get the existing category ID
        //        string getCategorySql = "SELECT CategoryID FROM Category WHERE CategoryName = @CategoryName";
        //        using var getCatCmd = new SqlCommand(getCategorySql, conn, transaction);
        //        getCatCmd.Parameters.AddWithValue("@CategoryName", categoryName);

        //        var categoryIdObj = await getCatCmd.ExecuteScalarAsync();
        //        if (categoryIdObj == null)
        //            throw new Exception("Category does not exist.");

        //        int categoryId = Convert.ToInt32(categoryIdObj);

        //        // 2. Get existing subcategory names
        //        var existingSubNames = new List<string>();
        //        using (var getSubsCmd = new SqlCommand("SELECT SubCategoryName FROM SubCategory WHERE CategoryID = @CategoryID", conn, transaction))
        //        {
        //            getSubsCmd.Parameters.AddWithValue("@CategoryID", categoryId);
        //            using var reader = await getSubsCmd.ExecuteReaderAsync();
        //            while (await reader.ReadAsync())
        //            {
        //                existingSubNames.Add(reader.GetString(0));
        //            }
        //        }

        //        // 3. Insert new subcategories (not in DB) with States = 'A'
        //        foreach (var name in updatedSubCategoryNames.Except(existingSubNames))
        //        {
        //            using var insertCmd = new SqlCommand(@"
        //        INSERT INTO SubCategory (SubCategoryName, CategoryID, States) 
        //        VALUES (@Name, @CategoryID, 'A')", conn, transaction);

        //            insertCmd.Parameters.AddWithValue("@Name", name);
        //            insertCmd.Parameters.AddWithValue("@CategoryID", categoryId);
        //            await insertCmd.ExecuteNonQueryAsync();
        //        }

        //        // 4. Set States = 'I' for subcategories removed from input
        //        foreach (var name in existingSubNames.Except(updatedSubCategoryNames))
        //        {
        //            using var updateInactiveCmd = new SqlCommand(@"
        //        UPDATE SubCategory 
        //        SET States = 'I' 
        //        WHERE SubCategoryName = @Name AND CategoryID = @CategoryID", conn, transaction);

        //            updateInactiveCmd.Parameters.AddWithValue("@Name", name);
        //            updateInactiveCmd.Parameters.AddWithValue("@CategoryID", categoryId);
        //            await updateInactiveCmd.ExecuteNonQueryAsync();
        //        }

        //        // 5. Set States = 'A' for subcategories in the input list (even if already in DB)
        //        foreach (var name in updatedSubCategoryNames)
        //        {
        //            using var updateActiveCmd = new SqlCommand(@"
        //        UPDATE SubCategory 
        //        SET States = 'A' 
        //        WHERE SubCategoryName = @Name AND CategoryID = @CategoryID", conn, transaction);

        //            updateActiveCmd.Parameters.AddWithValue("@Name", name);
        //            updateActiveCmd.Parameters.AddWithValue("@CategoryID", categoryId);
        //            await updateActiveCmd.ExecuteNonQueryAsync();
        //        }

        //        await transaction.CommitAsync();
        //        return true;
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}
        public async Task<object?> GetSubCategoryWithProductsByIdAsync(int subCategoryId)
        {
            string query = @"
SELECT
    p.ProductID,
    p.Name AS ProductName,
    p.Description,
    p.Price,
    p.Stock,
    p.Status,
    p.Slug,
    p.SubCategoryID,
    c.ColorName,
    s.SizeName,
    pi.ImageData
FROM Product p
LEFT JOIN ProductColor pc ON p.ProductID = pc.ProductID
LEFT JOIN Color c ON pc.ColorID = c.ColorID
LEFT JOIN ProductSize ps ON p.ProductID = ps.ProductID
LEFT JOIN Size s ON ps.SizeID = s.SizeID
LEFT JOIN ProductImage pi ON p.ProductID = pi.ProductId
WHERE p.SubCategoryID = @SubCategoryID";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId);

            var productsDict = new Dictionary<int, dynamic>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(reader.GetOrdinal("ProductID")))
                    continue;

                int productId = reader.GetInt32(reader.GetOrdinal("ProductID"));

                if (!productsDict.ContainsKey(productId))
                {
                    productsDict[productId] = new
                    {
                        id = productId,
                        name = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? "" : reader.GetString(reader.GetOrdinal("ProductName")),
                        description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                        price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Price")),
                        stock = reader.IsDBNull(reader.GetOrdinal("Stock")) ? 0 : reader.GetInt32(reader.GetOrdinal("Stock")),
                        status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "" : reader.GetString(reader.GetOrdinal("Status")),
                        slug = reader.IsDBNull(reader.GetOrdinal("Slug")) ? "" : reader.GetString(reader.GetOrdinal("Slug")),
                        colors = new HashSet<string>(),
                        sizes = new HashSet<string>(),
                        images = new HashSet<string>()
                    };
                }

                var product = productsDict[productId];

                if (!reader.IsDBNull(reader.GetOrdinal("ColorName")))
                    product.colors.Add(reader.GetString(reader.GetOrdinal("ColorName")));

                if (!reader.IsDBNull(reader.GetOrdinal("SizeName")))
                    product.sizes.Add(reader.GetString(reader.GetOrdinal("SizeName")));

                if (!reader.IsDBNull(reader.GetOrdinal("ImageData")))
                {
                    string fullPath = reader.GetString(reader.GetOrdinal("ImageData"));
                    string relativePath = fullPath.Replace(@"C:\inetpub\wwwroot\liarabackend\wwwroot\uploads\products", "").Replace(@"\", "/");
                    string ImageData = "https://liarabackend.dockyardsoftware.com" + relativePath;
                    product.images.Add(ImageData);
                }
            }

            return new
            {
                subCategoryId = subCategoryId,
                products = productsDict.Values
            };
        }

        public async Task<bool> UpdateSubCategoriesAsync(string categoryName, UpdateSubCategoryModel model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Get the existing category ID
                string getCategorySql = "SELECT CategoryID FROM Category WHERE CategoryName = @CategoryName";
                using var getCatCmd = new SqlCommand(getCategorySql, conn, transaction);
                getCatCmd.Parameters.AddWithValue("@CategoryName", categoryName);

                var categoryIdObj = await getCatCmd.ExecuteScalarAsync();
                if (categoryIdObj == null)
                    throw new Exception("Category does not exist.");

                int categoryId = Convert.ToInt32(categoryIdObj);

                // 2. Get existing subcategory names for this category
                var existingSubNames = new List<string>();
                using (var getSubsCmd = new SqlCommand("SELECT SubCategoryName FROM SubCategory WHERE CategoryID = @CategoryID", conn, transaction))
                {
                    getSubsCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    using var reader = await getSubsCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingSubNames.Add(reader.GetString(0));
                    }
                }

                // 3. Insert new subcategories with provided state
                foreach (var name in model.SubCategoryNames.Except(existingSubNames))
                {
                    using var insertCmd = new SqlCommand(@"
                INSERT INTO SubCategory (SubCategoryName, CategoryID, States) 
                VALUES (@Name, @CategoryID, @States)", conn, transaction);

                    insertCmd.Parameters.AddWithValue("@Name", name);
                    insertCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    insertCmd.Parameters.AddWithValue("@States", model.States);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // 4. Update states for all submitted subcategories
                foreach (var name in model.SubCategoryNames)
                {
                    using var updateStateCmd = new SqlCommand(@"
                UPDATE SubCategory 
                SET States = @States 
                WHERE SubCategoryName = @Name AND CategoryID = @CategoryID", conn, transaction);

                    updateStateCmd.Parameters.AddWithValue("@Name", name);
                    updateStateCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    updateStateCmd.Parameters.AddWithValue("@States", model.States);
                    await updateStateCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        // Get category by id with subcategories
        public async Task<object> GetCategoryByIdAsync(int id)
        {
            string query = @"
                SELECT c.CategoryID, c.CategoryName, sc.SubCategoryID, sc.SubCategoryName
                FROM Category c
                LEFT JOIN SubCategory sc ON c.CategoryID = sc.CategoryID
                WHERE c.CategoryID = @CategoryID
                ORDER BY sc.SubCategoryID";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CategoryID", id);

            using var reader = await cmd.ExecuteReaderAsync();

            int? categoryId = null;
            string categoryName = null;
            List<object> subCategories = new List<object>();

            while (await reader.ReadAsync())
            {
                if (categoryId == null)
                {
                    categoryId = reader.GetInt32(0);
                    categoryName = reader.GetString(1);
                }

                if (!reader.IsDBNull(2))
                {
                    subCategories.Add(new
                    {
                        id = reader.GetInt32(2),
                        name = reader.GetString(3)
                    });
                }
            }

            if (categoryId == null)
            {
                return null;
            }

            return new
            {
                id = categoryId,
                name = categoryName,
                subCategories = subCategories
            };
        }

        // Get subcategories for a category
        public async Task<List<object>> GetSubCategoriesAsync(int categoryId)
        {
            var subCategories = new List<object>();

            string query = @"
        SELECT SubCategoryID, SubCategoryName, States
        FROM SubCategory
        WHERE CategoryID = @CategoryID";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CategoryID", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                subCategories.Add(new
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    state = reader.IsDBNull(2) ? null : reader.GetString(2)  // Handle possible nulls
                });
            }

            return subCategories;
        }


        //// Delete category and its subcategories
        //public async Task<bool> DeleteCategoryAsync(int id)
        //{
        //    using var conn = new SqlConnection(_connectionString);
        //    await conn.OpenAsync();

        //    using var transaction = conn.BeginTransaction();

        //    try
        //    {
        //        // Delete subcategories
        //        string deleteSubCategoriesQuery = "DELETE FROM SubCategory WHERE CategoryID = @CategoryID";
        //        using var cmdSub = new SqlCommand(deleteSubCategoriesQuery, conn, transaction);
        //        cmdSub.Parameters.AddWithValue("@CategoryID", id);
        //        await cmdSub.ExecuteNonQueryAsync();

        //        // Delete category
        //        string deleteCategoryQuery = "DELETE FROM Category WHERE CategoryID = @CategoryID";
        //        using var cmdCat = new SqlCommand(deleteCategoryQuery, conn, transaction);
        //        cmdCat.Parameters.AddWithValue("@CategoryID", id);

        //        int rowsAffected = await cmdCat.ExecuteNonQueryAsync();

        //        if (rowsAffected == 0)
        //        {
        //            transaction.Rollback();
        //            return false;
        //        }

        //        transaction.Commit();
        //        return true;
        //    }
        //    catch
        //    {
        //        transaction.Rollback();
        //        throw;
        //    }
        //}

        public async Task<bool> SetSubCategoryStateAsync(int subCategoryId, string state)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                string updateSubCategoryQuery = @"
            UPDATE SubCategory
            SET States = @State
            WHERE SubCategoryID = @SubCategoryID";

                using var cmd = new SqlCommand(updateSubCategoryQuery, conn, transaction);
                cmd.Parameters.AddWithValue("@State", state);
                cmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<bool> SetCategoryStateAsync(int categoryId, string state)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                // If setting to Inactive, first check for active subcategories
                if (state == "I")
                {
                    string checkActiveSubQuery = @"
          SELECT COUNT(*)
          FROM SubCategory
          WHERE CategoryID = @CategoryID AND States = 'A'";
                    using var checkCmd = new SqlCommand(checkActiveSubQuery, conn, transaction);
                    checkCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    int activeSubCount = (int)await checkCmd.ExecuteScalarAsync();
                    if (activeSubCount > 0)
                    {
                        // Rollback not strictly necessary since no changes made yet
                        await transaction.RollbackAsync();
                        return false; // Cannot set category to inactive if any subcategories are active
                    }
                }
                // Update subcategories
                string updateSubCategoriesQuery = @"
      UPDATE SubCategory
      SET States = @State
      WHERE CategoryID = @CategoryID";
                using var cmdSub = new SqlCommand(updateSubCategoriesQuery, conn, transaction);
                cmdSub.Parameters.AddWithValue("@State", state);
                cmdSub.Parameters.AddWithValue("@CategoryID", categoryId);
                await cmdSub.ExecuteNonQueryAsync();
                // Update category
                string updateCategoryQuery = @"
      UPDATE Category
      SET States = @State
      WHERE CategoryID = @CategoryID";
                using var cmdCat = new SqlCommand(updateCategoryQuery, conn, transaction);
                cmdCat.Parameters.AddWithValue("@State", state);
                cmdCat.Parameters.AddWithValue("@CategoryID", categoryId);
                int rowsAffected = await cmdCat.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



    }
}
