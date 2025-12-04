using Microsoft.AspNetCore.Mvc;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnRequestController : ControllerBase
    {
        private readonly DAReturnRequest _dataAccess;

        public ReturnRequestController(IConfiguration configuration)
        {
            _dataAccess = new DAReturnRequest(configuration);
        }

        //[HttpPost("submit")]
        //public async Task<IActionResult> SubmitReturn([FromBody] ReturnRequestModel dto)
        //{
        //    bool success = await _dataAccess.SubmitReturnAsync(dto);
        //    if (success)
        //        return Ok(new { message = "Return request submitted successfully." });
        //    return BadRequest(new { message = "Failed to submit return request." });
        //}
        //[HttpGet("by-order/{orderId}")]
        //public async Task<IActionResult> GetReturnsByOrderId(int orderId)
        //{
        //    var returns = await _dataAccess.GetReturnsByOrderIdAsync(orderId);
        //    return Ok(returns);
        //}
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReturn([FromForm] ReturnRequestModel dto, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Save image in wwwroot/uploads/returns/
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "returns");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"return_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Save relative path (for frontend use)
                    dto.ImageUrl = $"/uploads/returns/{uniqueFileName}";
                }

                bool success = await _dataAccess.SubmitReturnAsync(dto);

                if (success)
                    return Ok(new { message = "Return request submitted successfully." });

                return BadRequest(new { message = "Failed to submit return request." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in SubmitReturn Controller: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllReturns()
        {
            var returns = await _dataAccess.GetAllReturnsAsync();
            return Ok(returns);
        }
        //[HttpGet("GetProductsByOrder")]
        //public async Task<IActionResult> GetProductsByOrder(int orderId)
        //{
        //    var products = await _dataAccess.GetProductsByOrderIdAsync(orderId);
        //    if (products == null || products.Count == 0)
        //        return NotFound(new { message = "No products found for this order." });

        //    return Ok(products);
        //}
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetReturnsByOrderId(int orderId)
        {
            var results = await _dataAccess.GetReturnsByOrderIdAsync(orderId);

            if (results == null || results.Count == 0)
                return NotFound(new { message = "No return requests found for this order." });

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Prefix ImageUrl with base URL
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.ImageUrl) && !item.ImageUrl.StartsWith("http"))
                {
                    item.ImageUrl = baseUrl + item.ImageUrl;
                }
            }

            return Ok(results);
        }


        [HttpPut("update-status/{returnId}")]
        public async Task<IActionResult> UpdateStatus(int returnId, [FromBody] string newStatus)
        {
            bool success = await _dataAccess.UpdateReturnStatusAsync(returnId, newStatus);
            if (success)
                return Ok(new { message = "Status updated successfully." });
            return BadRequest(new { message = "Failed to update status." });
        }
    }
}
