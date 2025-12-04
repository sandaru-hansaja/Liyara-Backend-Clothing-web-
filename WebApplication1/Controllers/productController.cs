

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using WebApplication1.Models;

//namespace WebApplication1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ProductController : ControllerBase
//    {
//        private readonly LiaraDbContext _context;
//        private readonly IWebHostEnvironment _environment;

//        public ProductController(LiaraDbContext context, IWebHostEnvironment environment)
//        {
//            _context = context;
//            _environment = environment;
//        }

//        /// <summary>
//        /// GET api/Product/Search?keyword={keyword}
//        /// Filters products whose Name, CategoryName, or SubCategoryName contains the keyword (case-insensitive).
//        /// If keyword is omitted or empty, returns all products.
//        /// </summary>
//        [HttpGet("Search")]
//        public async Task<IActionResult> SearchProducts([FromQuery] string? keyword)
//        {
//            try
//            {
//                IQueryable<Product> query = _context.Product
//                    .Include(p => p.ProductImage)
//                    .Include(p => p.ProductColor).ThenInclude(pc => pc.Color)
//                    .Include(p => p.ProductSize).ThenInclude(ps => ps.Size)
//                    .Include(p => p.Category)
//                    .Include(p => p.SubCategory)
//                    .AsQueryable();

//                if (!string.IsNullOrWhiteSpace(keyword))
//                {
//                    string normalized = keyword.Trim().ToLower();
//                    query = query.Where(p =>
//                        p.Name.ToLower().Contains(normalized) ||
//                        (p.Category != null && p.Category.CategoryName.ToLower().Contains(normalized)) ||
//                        (p.SubCategory != null && p.SubCategory.SubCategoryName.ToLower().Contains(normalized))
//                    );
//                }

//                var products = await query.ToListAsync();
//                if (products == null || products.Count == 0)
//                    return NotFound("No products match the search criteria.");

//                var result = products.Select(product => new
//                {
//                    ProductID = product.ProductID,
//                    Name = product.Name ?? "No Name Available",
//                    CategoryID = product.CategoryID,
//                    CategoryName = product.Category?.CategoryName ?? "Uncategorized",
//                    SubCategoryID = product.SubCategoryID,
//                    SubCategoryName = product.SubCategory?.SubCategoryName ?? "No SubCategory",
//                    Stock = product.Stock,
//                    Status = product.Stock == 0 ? "Sold Out" : "Available",
//                    Price = product.Price ?? 0,
//                    Colors = product.ProductColor.Select(pc => new
//                    {
//                        ColorID = pc.Color.ColorID,
//                        Name = pc.Color.ColorName
//                    }).ToList(),
//                    Sizes = product.ProductSize.Select(ps => new
//                    {
//                        SizeID = ps.Size.SizeID,
//                        Name = ps.Size.SizeName
//                    }).ToList(),
//                    Images = product.ProductImage.Select(img => new
//                    {
//                        img.ImageID,
//                        // Changed to lowercase to match frontend
//                        base64Image = img.ImageData != null ? Convert.ToBase64String(img.ImageData) : null
//                    }).ToList(),
//                    // Add slug field
//                    slug = product.Slug
//                }).ToList();

//                Console.WriteLine("Search Response JSON: " + JsonConvert.SerializeObject(result));
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error in SearchProducts: " + ex.Message);
//                return StatusCode(500, "An error occurred while searching products.");
//            }
//        }

//        /// <summary>
//        /// GET api/Product/Getproduct?categoryId={id}&colorIds={ids}&sizeIds={ids}
//        /// Filters by categoryId, colorIds, and sizeIds as before.
//        /// </summary>
//        [HttpGet("Getproduct")]
//        public async Task<IActionResult> GetProducts(
//            [FromQuery] int? categoryId = null,
//            [FromQuery(Name = "colorIds")] int[]? colorIds = null,
//            [FromQuery(Name = "sizeIds")] int[]? sizeIds = null)
//        {
//            try
//            {
//                var query = _context.Product
//                    .Include(p => p.ProductImage)
//                    .Include(p => p.ProductColor).ThenInclude(pc => pc.Color)
//                    .Include(p => p.ProductSize).ThenInclude(ps => ps.Size)
//                    .Include(p => p.Category)
//                    .AsQueryable();

//                if (categoryId.HasValue)
//                    query = query.Where(p => p.CategoryID == categoryId.Value);

//                if (colorIds?.Any() ?? false)
//                    query = query.Where(p => p.ProductColor.Any(pc => colorIds.Contains(pc.ColorID)));

//                if (sizeIds?.Any() ?? false)
//                    query = query.Where(p => p.ProductSize.Any(ps => sizeIds.Contains(ps.SizeID)));

//                var products = await query.ToListAsync();
//                if (products == null || products.Count == 0)
//                    return NotFound("No products found with given filters.");

//                var result = products.Select(product => new
//                {
//                    ProductID = product.ProductID,
//                    Name = product.Name,
//                    CategoryID = product.CategoryID,
//                    CategoryName = product.Category != null ? product.Category.CategoryName : "Unknown",
//                    SubCategoryID = product.SubCategoryID,
//                    Stock = product.Stock,
//                    Price = product.Price ?? 0,
//                    Colors = product.ProductColor.Select(pc => pc.Color.ColorID).ToList(),
//                    Sizes = product.ProductSize.Select(ps => ps.Size.SizeID).ToList(),
//                    Images = product.ProductImage.Select(img => new
//                    {
//                        img.ImageID,
//                        Base64Image = img.ImageData != null ? Convert.ToBase64String(img.ImageData) : null
//                    }).ToList()
//                }).ToList();

//                Console.WriteLine("Getproduct Response JSON: " + JsonConvert.SerializeObject(result));
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error in GetProducts: " + ex.Message);
//                return StatusCode(500, "An error occurred while fetching products.");
//            }
//        }

//        /// <summary>
//        /// GET api/Product/GetAllProducts
//        /// Returns all products with details, colors, sizes, images, etc.
//        /// </summary>
//        [HttpGet("GetAllProducts")]
//        public async Task<IActionResult> GetAllProducts()
//        {
//            try
//            {
//                var products = await _context.Product
//                    .Include(p => p.ProductImage)
//                    .Include(p => p.ProductColor).ThenInclude(pc => pc.Color)
//                    .Include(p => p.ProductSize).ThenInclude(ps => ps.Size)
//                    .Include(p => p.Category)
//                    .ToListAsync();

//                if (products == null || products.Count == 0)
//                    return NotFound("No products found.");

//                var result = products.Select(product => new
//                {
//                    ProductID = product.ProductID,
//                    Name = product.Name ?? "No Name Available",
//                    CategoryID = product.CategoryID,
//                    CategoryName = product.Category?.CategoryName ?? "Uncategorized",
//                    SubCategoryID = product.SubCategoryID,
//                    Stock = product.Stock,
//                    Status = product.Stock == 0 ? "Sold Out" : "Available",
//                    Price = product.Price ?? 0,
//                    Colors = product.ProductColor.Select(c => new
//                    {
//                        ColorID = c.Color.ColorID,
//                        Name = c.Color.ColorName
//                    }).ToList(),
//                    Sizes = product.ProductSize.Select(ps => new
//                    {
//                        SizeID = ps.Size.SizeID,
//                        Name = ps.Size.SizeName
//                    }).ToList(),
//                    Images = product.ProductImage.Select(img => new
//                    {
//                        img.ImageID,
//                        Base64Image = img.ImageData != null ? Convert.ToBase64String(img.ImageData) : null
//                    }).ToList()
//                }).ToList();

//                Console.WriteLine("GetAllProducts Response JSON: " + JsonConvert.SerializeObject(result));
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error in GetAllProducts: " + ex.Message);
//                return StatusCode(500, "An error occurred while fetching products.");
//            }
//        }

//        /// <summary>
//        /// GET api/Product/GetLatestProducts
//        /// Returns the 8 most recently added products.
//        /// </summary>
//        [HttpGet("GetLatestProducts")]
//        public async Task<IActionResult> GetLatestProducts()
//        {
//            try
//            {
//                var products = await _context.Product
//                    .Include(p => p.ProductImage)
//                    .Include(p => p.ProductColor).ThenInclude(pc => pc.Color)
//                    .Include(p => p.ProductSize).ThenInclude(ps => ps.Size)
//                    .Include(p => p.Category)
//                    .OrderByDescending(p => p.ProductID)
//                    .Take(8)
//                    .ToListAsync();

//                if (products == null || products.Count == 0)
//                    return NotFound("No products found.");

//                var result = products.Select(product => new
//                {
//                    ProductID = product.ProductID,
//                    Name = product.Name ?? "No Name Available",
//                    CategoryID = product.CategoryID,
//                    CategoryName = product.Category != null ? product.Category.CategoryName : "Uncategorized",
//                    SubCategoryID = product.SubCategoryID,
//                    Stock = product.Stock,
//                    Price = product.Price ?? 0,
//                    Colors = product.ProductColor.Select(pc => new
//                    {
//                        ColorID = pc.Color.ColorID,
//                        Name = pc.Color.ColorName
//                    }).ToList(),
//                    Sizes = product.ProductSize.Select(ps => new
//                    {
//                        SizeID = ps.Size.SizeID,
//                        Name = ps.Size.SizeName
//                    }).ToList(),
//                    Images = product.ProductImage.Select(img => new
//                    {
//                        img.ImageID,
//                        Base64Image = img.ImageData != null ? Convert.ToBase64String(img.ImageData) : null
//                    }).ToList()
//                }).ToList();

//                Console.WriteLine("GetLatestProducts Response JSON: " + JsonConvert.SerializeObject(result));
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error in GetLatestProducts: " + ex.Message);
//                return StatusCode(500, "An error occurred while fetching products.");
//            }
//        }

//        /// <summary>
//        /// GET api/Product/{id}
//        /// Returns a single product by its ID.
//        /// </summary>
//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetProductById(int id)
//        {
//            var product = await _context.Product
//                .Where(p => p.ProductID == id)
//                .Select(p => new
//                {
//                    id = p.ProductID,
//                    name = p.Name,
//                    price = p.Price,
//                    colors = p.ProductColor.Select(pc => new
//                    {
//                        colorID = pc.Color.ColorID,
//                        color = pc.Color.ColorName
//                    }).ToList(),
//                    images = _context.ProductImage
//                        .Where(img => img.ProductId == id)
//                        .Select(img => Convert.ToBase64String(img.ImageData))
//                        .ToList()
//                })
//                .FirstOrDefaultAsync();

//            if (product == null)
//                return NotFound(new { message = "Product not found" });

//            return Ok(product);
//        }

//        /// <summary>
//        /// DELETE api/Product/DeleteProduct/{id}
//        /// Deletes a product by its ID.
//        /// </summary>
//        [HttpDelete("DeleteProduct/{id}")]
//        public async Task<IActionResult> DeleteProduct(int id)
//        {
//            try
//            {
//                var product = await _context.Product.FindAsync(id);
//                if (product == null)
//                    return NotFound(new { message = "Product not found" });

//                _context.Product.Remove(product);
//                await _context.SaveChangesAsync();

//                return Ok(new { message = "Product deleted successfully" });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error in DeleteProduct: " + ex.Message);
//                return StatusCode(500, new { message = "Error deleting product", error = ex.Message });
//            }
//        }

//        /// <summary>
//        /// POST api/Product/Addproduct
//        /// Adds a new product with images, colors, sizes, etc.
//        /// </summary>
//        [HttpPost("Addproduct")]
//        public async Task<IActionResult> AddProducts([FromForm] ProductDto productDto)
//        {
//            if (productDto == null ||
//                string.IsNullOrEmpty(productDto.Name) ||
//                string.IsNullOrEmpty(productDto.CategoryName) ||
//                string.IsNullOrEmpty(productDto.SubCategoryName) ||
//                productDto.Images == null || !productDto.Images.Any() ||
//                productDto.Colors == null || !productDto.Colors.Any() ||
//                productDto.Sizes == null || !productDto.Sizes.Any())
//            {
//                return BadRequest("All product details, at least one image, color, and size are required.");
//            }

//            // Validate Category
//            var category = await _context.Category
//                .FirstOrDefaultAsync(c => c.CategoryName.Trim().ToLower() == productDto.CategoryName.Trim().ToLower());
//            if (category == null)
//                return BadRequest("Invalid Category Name.");

//            // Validate SubCategory
//            var subCategory = await _context.SubCategory
//                .FirstOrDefaultAsync(sc =>
//                    sc.SubCategoryName.Trim().ToLower() == productDto.SubCategoryName.Trim().ToLower() &&
//                    sc.CategoryID == category.CategoryID);
//            if (subCategory == null)
//                return BadRequest("Invalid SubCategory Name for the given Category.");

//            // Validate Colors
//            var normalizedColors = productDto.Colors
//                .Select(cn => cn.Trim().ToLower())
//                .ToList();
//            var colorEntities = await _context.Color
//                .Where(c => normalizedColors.Contains(c.ColorName.Trim().ToLower()))
//                .ToListAsync();
//            if (colorEntities.Count != normalizedColors.Count)
//                return BadRequest("One or more selected colors do not exist in the database.");

//            // Validate Sizes
//            var normalizedSizes = productDto.Sizes
//                .Select(sn => sn.Trim().ToLower())
//                .ToList();
//            var sizeEntities = await _context.Size
//                .Where(s => normalizedSizes.Contains(s.SizeName.Trim().ToLower()))
//                .ToListAsync();
//            if (sizeEntities.Count != normalizedSizes.Count)
//                return BadRequest("One or more selected sizes do not exist in the database.");

//            // Create Product
//            var product = new Product
//            {
//                Name = productDto.Name,
//                CategoryID = category.CategoryID,
//                SubCategoryID = subCategory.SubCategoryID,
//                Stock = productDto.Stock,
//                Price = productDto.Price,
//                Status = productDto.Stock > 0 ? "Available" : "Sold Out"
//            };

//            _context.Product.Add(product);
//            await _context.SaveChangesAsync(); // Save to get ProductID

//            // Associate Colors
//            var productColors = colorEntities.Select(color => new ProductColor
//            {
//                ProductID = product.ProductID,
//                ColorID = color.ColorID
//            }).ToList();
//            _context.ProductColor.AddRange(productColors);

//            // Associate Sizes
//            var productSizes = sizeEntities.Select(size => new ProductSize
//            {
//                ProductID = product.ProductID,
//                SizeID = size.SizeID ?? 0
//            }).ToList();
//            _context.ProductSize.AddRange(productSizes);

//            await _context.SaveChangesAsync(); // Save associations

//            // Save product images
//            foreach (var image in productDto.Images)
//            {
//                if (image.Length > 0)
//                {
//                    using var memoryStream = new MemoryStream();
//                    await image.CopyToAsync(memoryStream);
//                    var productImage = new ProductImage
//                    {
//                        ProductId = product.ProductID,
//                        ImageData = memoryStream.ToArray()
//                    };
//                    _context.ProductImage.Add(productImage);
//                }
//            }

//            await _context.SaveChangesAsync(); // Save images

//            return Ok(new { message = "Product added successfully!" });
//        }

//        /// <summary>
//        /// GET api/Product/GetProductDetails/{productId}
//        /// Returns detailed info for a single product, including category, subcategory, colors, sizes, images.
//        /// </summary>
//        [HttpGet("GetProductDetails/{productId}")]
//        public async Task<IActionResult> GetProductDetails(int productId)
//        {
//            var product = await _context.Product
//                .Include(p => p.Category)
//                .Include(p => p.SubCategory)
//                .Include(p => p.ProductColor).ThenInclude(pc => pc.Color)
//                .Include(p => p.ProductSize).ThenInclude(ps => ps.Size)
//                .Include(p => p.ProductImage)
//                .FirstOrDefaultAsync(p => p.ProductID == productId);

//            if (product == null)
//                return NotFound("Product not found.");

//            var productDetails = new
//            {
//                product.ProductID,
//                product.Name,
//                CategoryName = product.Category?.CategoryName,
//                SubCategoryName = product.SubCategory?.SubCategoryName,
//                product.Stock,
//                product.Price,
//                product.Status,
//                Colors = product.ProductColor.Select(pc => pc.Color.ColorName).ToList(),
//                Sizes = product.ProductSize.Select(ps => ps.Size.SizeName).ToList(),
//                Images = product.ProductImage.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
//            };

//            return Ok(productDetails);
//        }

//        /// <summary>
//        /// PUT api/Product/UpdateProduct/{id}
//        /// Updates an existing product (name, category, subcategory, stock, price, colors, sizes, optional images).
//        /// </summary>
//        [HttpPut("UpdateProduct/{id}")]
//        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
//        {
//            if (dto == null)
//                return BadRequest("Product data cannot be null.");

//            if (string.IsNullOrWhiteSpace(dto.Name))
//                return BadRequest("Product name is required.");

//            // Find existing product
//            var existingProduct = await _context.Product.FindAsync(id);
//            if (existingProduct == null)
//                return NotFound("Product not found.");

//            // Validate Category
//            if (string.IsNullOrWhiteSpace(dto.CategoryName))
//                return BadRequest("Category name is required.");

//            var category = await _context.Category
//                .FirstOrDefaultAsync(c => c.CategoryName.Trim().ToLower() == dto.CategoryName.Trim().ToLower());
//            if (category == null)
//                return BadRequest("Invalid Category Name.");

//            // Validate SubCategory
//            if (string.IsNullOrWhiteSpace(dto.SubCategoryName))
//                return BadRequest("SubCategory name is required.");

//            var subCategory = await _context.SubCategory
//                .FirstOrDefaultAsync(sc =>
//                    sc.SubCategoryName.Trim().ToLower() == dto.SubCategoryName.Trim().ToLower() &&
//                    sc.CategoryID == category.CategoryID);
//            if (subCategory == null)
//                return BadRequest("Invalid SubCategory Name for the given Category.");

//            // Validate Colors
//            if (dto.Colors == null || !dto.Colors.Any())
//                return BadRequest("At least one color is required.");

//            var normalizedRequestedColors = dto.Colors
//                .Select(cn => cn.Trim().ToLower())
//                .ToList();

//            var colorEntities = await _context.Color
//                .Where(c => normalizedRequestedColors.Contains(c.ColorName.Trim().ToLower()))
//                .ToListAsync();
//            if (colorEntities.Count != normalizedRequestedColors.Count)
//                return BadRequest("One or more selected colors do not exist.");

//            // Validate Sizes
//            if (dto.Sizes == null || !dto.Sizes.Any())
//                return BadRequest("At least one size is required.");

//            var normalizedRequestedSizes = dto.Sizes
//                .Select(sn => sn.Trim().ToLower())
//                .ToList();

//            var sizeEntities = await _context.Size
//                .Where(s => normalizedRequestedSizes.Contains(s.SizeName.Trim().ToLower()))
//                .ToListAsync();
//            if (sizeEntities.Count != normalizedRequestedSizes.Count)
//                return BadRequest("One or more selected sizes do not exist.");

//            // Update product fields
//            existingProduct.Name = dto.Name;
//            existingProduct.CategoryID = category.CategoryID;
//            existingProduct.SubCategoryID = subCategory.SubCategoryID;
//            existingProduct.Stock = dto.Stock;
//            existingProduct.Price = dto.Price;
//            existingProduct.Status = dto.Stock > 0 ? "Available" : "Sold Out";

//            // (Optional) Handle new images if provided
//            if (dto.Images != null && dto.Images.Any())
//            {
//                // Example: remove existing images, then add new ones.
//                var existingImages = _context.ProductImage.Where(pi => pi.ProductId == id);
//                _context.ProductImage.RemoveRange(existingImages);

//                foreach (var image in dto.Images)
//                {
//                    if (image.Length > 0)
//                    {
//                        using var memoryStream = new MemoryStream();
//                        await image.CopyToAsync(memoryStream);
//                        var productImage = new ProductImage
//                        {
//                            ProductId = id,
//                            ImageData = memoryStream.ToArray()
//                        };
//                        _context.ProductImage.Add(productImage);
//                    }
//                }
//            }

//            // Replace Colors
//            var oldColors = _context.ProductColor.Where(pc => pc.ProductID == id);
//            _context.ProductColor.RemoveRange(oldColors);

//            var newColors = colorEntities.Select(color => new ProductColor
//            {
//                ProductID = id,
//                ColorID = color.ColorID
//            });
//            _context.ProductColor.AddRange(newColors);

//            // Replace Sizes
//            var oldSizes = _context.ProductSize.Where(ps => ps.ProductID == id);
//            _context.ProductSize.RemoveRange(oldSizes);

//            var newSizes = sizeEntities.Select(size => new ProductSize
//            {
//                ProductID = id,
//                SizeID = size.SizeID ?? 0
//            });
//            _context.ProductSize.AddRange(newSizes);

//            await _context.SaveChangesAsync();

//            return Ok(new { message = "Product updated successfully!" });
//        }
//    }

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DAProduct _daProduct;
        private readonly IWebHostEnvironment _environment;

        public ProductController(IConfiguration config, IWebHostEnvironment environment)
        {
            _daProduct = new DAProduct(config);
            _environment = environment;
        }

        [HttpGet("Search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? keyword)
        {
            try
            {
                var products = await _daProduct.SearchProductsAsync(keyword?.Trim());
                if (products == null || products.Count == 0)
                    return NotFound("No products match the search criteria.");

                return Ok(products);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Error in SearchProducts: " + ex.Message);
                return StatusCode(500, "An error occurred while searching products.");
            }
        }

        [HttpGet("Getproduct")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId = null,
            [FromQuery(Name = "colorIds")] int[]? colorIds = null,
            [FromQuery(Name = "sizeIds")] int[]? sizeIds = null)
        {
            try
            {
                var products = await _daProduct.GetProductsAsync(categoryId, colorIds, sizeIds);
                if (products == null || products.Count == 0)
                    return NotFound("No products found with given filters.");

                return Ok(products);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Error in GetProducts: " + ex.Message);
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _daProduct.GetAllProductsAsync();
                if (products == null || products.Count == 0)
                    return NotFound("No products found.");

                return Ok(products);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Error in GetAllProducts: " + ex.Message);
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }

        //[HttpGet("GetAllOrders")]
        //public async Task<IActionResult> GetAllOrders()
        //{
        //    try
        //    {
        //        var orders = await _daProduct.GetAllOrdersAsync();
        //        if (orders == null || orders.Count == 0)
        //            return NotFound("No products found.");

        //        return Ok(orders);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        System.Console.WriteLine("Error in GetAllProducts: " + ex.Message);
        //        return StatusCode(500, "An error occurred while fetching products.");
        //    }
        //}

        [HttpGet("GetLatestProducts")]
        public async Task<IActionResult> GetLatestProducts()
        {
            try
            {
                var products = await _daProduct.GetLatestProductsAsync();
                if (products == null || products.Count == 0)
                    return NotFound("No products found.");

                return Ok(products);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Error in GetLatestProducts: " + ex.Message);
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }

        [HttpGet("GetProductDetails/{productId}")]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            try
            {
                var product = await _daProduct.GetProductDetailsAsync(productId);
                if (product == null)
                    return NotFound("Product not found.");

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching product: " + ex.Message);
            }
        }

        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var success = await _daProduct.DeleteProductAsync(id);
                if (!success)
                    return NotFound(new { message = "Product not found" });

                return Ok(new { message = "Product deleted successfully" });
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Error in DeleteProduct: " + ex.Message);
                return StatusCode(500, new { message = "Error deleting product", error = ex.Message });
            }
        }
        //[HttpPut("UpdateProduct")]
        //public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductModel productDto)
        //{
        //    try
        //    {
        //        bool updated = await _daProduct.UpdateProductAsync(productDto);
        //        if (updated)
        //            return Ok(new { message = "Product updated successfully!" });

        //        return StatusCode(500, "Failed to update product.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error in UpdateProduct: " + ex.Message);
        //        return StatusCode(500, new { message = "Error updating product", error = ex.Message });
        //    }
        //}
        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductModel productDto)
        {
            try
            {
               

                bool updated = await _daProduct.UpdateProductAsync(productDto);

                if (updated)
                    return Ok(new { message = "Product updated successfully!" });

                return StatusCode(500, "Failed to update product.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UpdateProduct: " + ex.Message);
                return StatusCode(500, new { message = "Error updating product", error = ex.Message });
            }
        }


        [HttpGet("api/category/names")]
        public async Task<IActionResult> GetCategories()
        {
            var list = await _daProduct.GetAllCategoryNamesAsync();
            return Ok(list);
        }

        [HttpGet("api/category/subcategories")]
        public async Task<IActionResult> GetSubCategories(string categoryName)
        {
            var list = await _daProduct.GetSubCategoryNamesByCategoryAsync(categoryName);
            return Ok(list);
        }
        [HttpPost("AddReview")]
        public async Task<IActionResult> AddReview([FromBody] ProductReviewModel review)
        {
            if (string.IsNullOrWhiteSpace(review.UserName) || string.IsNullOrWhiteSpace(review.ReviewText))
                return BadRequest("User name and review text are required.");

            bool added = await _daProduct.AddProductReviewAsync(review);
            return added ? Ok("Review added.") : StatusCode(500, "Failed to add review.");
        }

        [HttpGet("GetReviews/{productId}")]
        public async Task<IActionResult> GetReviews(int productId)
        {
            var reviews = await _daProduct.GetReviewsByProductIdAsync(productId);
            return Ok(reviews);
        }

        [HttpPost("ReactToReview/{reviewId}")]
        public async Task<IActionResult> ReactToReview(int reviewId, [FromQuery] bool like)
        {
            bool updated = await _daProduct.UpdateReviewReactionAsync(reviewId, like);
            return updated ? Ok("Reaction recorded.") : NotFound("Review not found.");
        }



        //[HttpPost("Addproduct")]
        //public async Task<IActionResult> AddProducts([FromForm] ProductDto productDto)
        //{
        //    try
        //    {
        //        bool added = await _daProduct.AddProductAsync(productDto);
        //        if (added)
        //            return Ok(new { message = "Product added successfully!" });

        //        return StatusCode(500, "Failed to add product.");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        System.Console.WriteLine("Error in AddProducts: " + ex.Message);
        //        return StatusCode(500, new { message = "Error adding product", error = ex.Message });
        //    }
        [HttpPost("Addproduct")]
        public async Task<IActionResult> AddProducts([FromForm] ProductModel productDto)
        {
            try
            {
                bool added = await _daProduct.AddProductAsync(productDto);

                if (added)
                    return Ok(new { message = "Product added successfully!" });

                return StatusCode(500, "Failed to add product.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddProducts: " + ex.Message);
                return StatusCode(500, new { message = "Error adding product", error = ex.Message });
            }
        }


        // DTO for creating a new product
        //public class ProductDto
        //{
        //    public string? Name { get; set; }
        //    public string? CategoryName { get; set; }
        //    public string? SubCategoryName { get; set; }
        //    public int Stock { get; set; }
        //    public decimal Price { get; set; }
        //    public List<IFormFile> Images { get; set; } = new();
        //    public List<string> Colors { get; set; } = new();
        //    public List<string>? ImageUrls { get; set; } = new List<string>();
        //    public List<string> Sizes { get; set; } = new();
        //    public int ProductID { get; set; }
        //    public string? Description { get; set; }

        //}

    }
}
