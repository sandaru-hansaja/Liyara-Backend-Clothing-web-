//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Security.Claims;
//using WebApplication1;
//using WebApplication1.Models;

//namespace WebApplication1.Controllers
//{


//    [ApiController]
//    [Route("api/[controller]")]
//    public class CartController : ControllerBase
//    {
//        private readonly LiaraDbContext _context;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public CartController(LiaraDbContext context, IHttpContextAccessor httpContextAccessor)
//        {
//            _context = context;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        private int GetUserId()
//        {
//            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
//            if (userIdClaim == null)
//                throw new Exception("User not authenticated");
//            return int.Parse(userIdClaim.Value);
//        }

//        // GET api/cart
//        [HttpGet]
//        [Authorize]
//        public async Task<ActionResult<List<CartItemDto>>> GetCart()
//        {
//            int userId = GetUserId();
//            var cartItems = await _context.CartItemDto
//                .Where(c => c.UserID == userId)
//                .ToListAsync();

//            return Ok(cartItems);
//        }

//        // POST api/cart
//        [HttpPost]
//        [Authorize]
//        public async Task<IActionResult> SaveCart([FromBody] List<CartItemDto> cartItems)
//        {
//            int userId = GetUserId();

//            // Remove existing cart items for user
//            var existingItems = _context.CartItemDto.Where(c => c.UserID == userId);
//            _context.CartItemDto.RemoveRange(existingItems);

//            // Add updated cart items with userId
//            foreach (var item in cartItems)
//            {
//                item.UserID = userId;
//                _context.CartItemDto.Add(item);
//            }

//            await _context.SaveChangesAsync();
//            return Ok();
//        }

//        // GET api/cart/orders  --> new endpoint to get previous orders
//        [HttpGet("orders")]
//        [Authorize]
//        public async Task<ActionResult<List<Orders>>> GetUserOrders()
//        {
//            int userId = GetUserId();

//            var orders = await _context.Orders
//                .Where(o => o.UserID == userId)
//                .Include(o => o.OrderItems)
//                .ToListAsync();

//            // Map entities to DTOs as needed
//            var orderDtos = orders.Select(o => new Orders
//            {
//                OrderID = o.OrderID,
//                OrderDate = o.OrderDate,
//                Total = o.Total,
//                OrderItems = o.OrderItems.Select(oi => new OrderItem
//                {
//                    ProductID = oi.ProductID,
//                    Quantity = oi.Quantity,
//                    Price = oi.Price,
//                    // other fields as needed
//                }).ToList()
//            }).ToList();

//            return Ok(orderDtos);
//        }
//    }

//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly DACart _cartService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(DACart cartService, IHttpContextAccessor httpContextAccessor)
        {
            _cartService = cartService;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new Exception("User not authenticated");
            return int.Parse(userIdClaim.Value);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<CartItemModel>>> GetCart()
        {
            int userId = GetUserId();
            var cartItems = await _cartService.GetCartItemsByUserId(userId);
            return Ok(cartItems);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveCart([FromBody] List<CartItemModel> cartItems)
        {
            int userId = GetUserId();
            await _cartService.SaveCartItems(userId, cartItems);
            return Ok();
        }

        [HttpGet("orders")]
        [Authorize]
        public async Task<ActionResult<List<OrderModel>>> GetUserOrders()
        {
            int userId = GetUserId();
            var orders = await _cartService.GetUserOrders(userId);
            return Ok(orders);
        }
    }
}

