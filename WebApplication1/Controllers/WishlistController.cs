using Microsoft.AspNetCore.Mvc;
using WebApplication1.DataAccess;
using WebApplication1.Models;

//[ApiController]
//[Route("api/[controller]")]
//public class WishlistController : ControllerBase
//{
//    private readonly DAWishlist _daWishlist;

//    public WishlistController(DAWishlist daWishlist)
//    {
//        _daWishlist = daWishlist;
//    }

//    [HttpPost("Add")]
//    public async Task<IActionResult> AddToWishlist([FromQuery] int customerId, [FromQuery] int productId)
//    {
//        var result = await _daWishlist.AddToWishlistAsync(customerId, productId);
//        if (result)
//            return Ok(new { message = "Product added to wishlist." });

//        return BadRequest(new { message = "Failed to add product to wishlist." });
//    }

//    [HttpGet("Get")]
//    public async Task<IActionResult> GetWishlist([FromQuery] int customerId)
//    {
//        var wishlist = await _daWishlist.GetWishlistByCustomerAsync(customerId);
//        return Ok(wishlist);
//    }

//    [HttpDelete("Remove")]
//    public async Task<IActionResult> RemoveFromWishlist([FromQuery] int customerId, [FromQuery] int productId)
//    {
//        var result = await _daWishlist.RemoveFromWishlistAsync(customerId, productId);
//        if (result)
//            return Ok(new { message = "Product removed from wishlist." });

//        return BadRequest(new { message = "Failed to remove product from wishlist." });
//    }
//}
[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly DAWishlist _daWishlist;

    public WishlistController(DAWishlist daWishlist)
    {
        _daWishlist = daWishlist;
    }

    // POST: api/Wishlist/Add?customerId=1&productId=5
    [HttpPost("Add")]
    public async Task<IActionResult> AddToWishlist([FromQuery] int customerId, [FromQuery] int productId)
    {
        var result = await _daWishlist.AddToWishlistAsync(customerId, productId);

        if (result)
            return Ok(new { message = "Product added to wishlist." });

        return BadRequest(new { message = "Failed to add product to wishlist." });
    }

    // GET: api/Wishlist/Get?customerId=1
    [HttpGet("Get")]
    public async Task<IActionResult> GetWishlist([FromQuery] int customerId)
    {
        var wishlist = await _daWishlist.GetWishlistByCustomerAsync(customerId);

        if (wishlist == null || wishlist.Count == 0)
            return NotFound(new { message = "No products found in wishlist." });

        return Ok(wishlist);
    }
    [HttpDelete("Remove")]
    public async Task<IActionResult> RemoveFromWishlist([FromQuery] int customerId, [FromQuery] int productId)
    {
        var result = await _daWishlist.RemoveFromWishlistAsync(customerId, productId);
        if (result)
            return Ok(new { message = "Product removed from wishlist." });

        return BadRequest(new { message = "Failed to remove product from wishlist." });
    }
}

