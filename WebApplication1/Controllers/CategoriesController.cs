using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly DACategory _daCategory;

        public CategoriesController(DACategory daCategory)
        {
            _daCategory = daCategory;
        }




        [HttpPut("SubCategory/{id}/state")]
        public async Task<IActionResult> UpdateSubCategoryState(int id, [FromQuery] string state)
        {
            bool success = await _daCategory.SetSubCategoryStateAsync(id, state);

            if (success)
                return Ok(new { message = "SubCategory state updated successfully." });

            return NotFound(new { message = "SubCategory not found or update failed." });
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            var categories = await _daCategory.GetCategoriesAsync();

            if (categories == null || categories.Count == 0)
            {
                return NotFound("No categories found.");
            }

            return Ok(categories);
        }

        [HttpPost("AddCategory")]
        public async Task<ActionResult<CategoryModel>> AddCategoryWithSubcategories([FromBody] CategoryModel category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return BadRequest("Category name is required.");
            }

            if (await _daCategory.CategoryExistsAsync(category.CategoryName))
            {
                return Conflict("Category already exists.");
            }

            if (category.SubCategory == null)
            {
                category.SubCategory = new List<SubCategoryModel>();
            }

            var addedCategory = await _daCategory.AddCategoryWithSubcategoriesAsync(category);

            return CreatedAtAction(nameof(GetCategories), new { id = addedCategory.CategoryID }, addedCategory);
        }
        [HttpPut("UpdateSubCategories")]
        public async Task<IActionResult> UpdateSubCategories([FromBody] UpdateSubCategoryModel request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName) ||
                request.SubCategoryNames == null ||
                string.IsNullOrWhiteSpace(request.States))
            {
                return BadRequest("Category name, subcategories, and states are required.");
            }

            try
            {
                // Pass the full model to the data access layer
                bool updated = await _daCategory.UpdateSubCategoriesAsync(request.CategoryName, request);
                return updated
                    ? Ok("Subcategories updated successfully.")
                    : NotFound("Category not found.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCategoryById(int id)
        {
            var category = await _daCategory.GetCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            return Ok(category);
        }

        [HttpGet("SubCategories/{categoryId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubCategories(int categoryId)
        {
            var subCategories = await _daCategory.GetSubCategoriesAsync(categoryId);

            if (subCategories.Count == 0)
            {
                return NotFound($"No subcategories found for Category ID {categoryId}.");
            }

            return Ok(subCategories);
        }

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCategory(int id)
        //{
        //    try
        //    {
        //        bool deleted = await _daCategory.DeleteCategoryAsync(id);

        //        if (!deleted)
        //        {
        //            return NotFound(new { message = "Category not found" });
        //        }

        //        return Ok(new { message = "Category and its subcategories deleted successfully" });
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}
        [HttpGet("SubCategoryById/{subCategoryId}")]
        public async Task<ActionResult<object>> GetSubCategoryById(int subCategoryId)
        {
            var subCategory = await _daCategory.GetSubCategoryWithProductsByIdAsync(subCategoryId);

            if (subCategory == null)
            {
                return NotFound($"No subcategory found with ID {subCategoryId}.");
            }

            return Ok(subCategory);
        }

        //[HttpPut("{id}/state")]
        //public async Task<IActionResult> UpdateCategoryState(int id, [FromQuery] string state)
        //{
        //    if (state != "A" && state != "I")
        //    {
        //        return BadRequest("Invalid state. Use 'A' for active or 'I' for inactive.");
        //    }

        //    try
        //    {
        //        bool result = await _daCategory.SetCategoryStateAsync(id, state);

        //        if (!result)
        //            return NotFound(new { message = "Category not found" });

        //        string msg = state == "I" ? "Category marked inactive successfully." : "Category marked active successfully.";
        //        return Ok(new { message = msg });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}
        [HttpPut("{id}/state")]
        public async Task<IActionResult> UpdateCategoryState(int id, [FromQuery] string state)
        {
            if (state != "A" && state != "I")
            {
                return BadRequest("Invalid state. Use 'A' for active or 'I' for inactive.");
            }
            try
            {
                bool result = await _daCategory.SetCategoryStateAsync(id, state);
                if (!result)
                {
                    if (state == "I")
                    {
                        return BadRequest(new
                        {
                            message = "Cannot deactivate category. One or more subcategories are still active."
                        });
                    }
                    return NotFound(new { message = "Category not found." });
                }
                string msg = state == "I"
                    ? "Category marked inactive successfully."
                    : "Category marked active successfully.";
                return Ok(new { message = msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


    }
}
