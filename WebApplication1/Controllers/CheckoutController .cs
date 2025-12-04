

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using WebApplication1.Models;
//using Microsoft.EntityFrameworkCore;

//namespace WebApplication1.Controllers
//{
//    [AllowAnonymous]
//    [ApiController]
//    [Route("api/checkout")]
//    public class CheckoutController : ControllerBase
//    {
//        private readonly LiaraDbContext _context;

//        public CheckoutController(LiaraDbContext context)
//        {
//            _context = context;
//        }



//        [HttpPost("PlaceOrder")]
//        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderViewDto orderDto)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest("Invalid order data");

//            // Save customer info
//            var customer = new PlaceOrderView
//            {
//                FirstName = orderDto.FirstName,
//                LastName = orderDto.LastName,
//                Address = orderDto.Address,
//                City = orderDto.City,
//                PostalCode = orderDto.PostalCode,
//                Phone = orderDto.Phone,
//                Email = orderDto.Email,
//                PaymentMethod = orderDto.PaymentMethod
//            };
//            _context.PlaceOrderView.Add(customer);
//            await _context.SaveChangesAsync();

//            // Build order items
//            var orderItems = new List<OrderItem>();
//            foreach (var itemDto in orderDto.CartItems)
//            {
//                var product = await _context.Product.FindAsync(itemDto.ProductID);
//                if (product == null)
//                    return BadRequest($"Product {itemDto.ProductID} not found.");
//                if (product.Stock < itemDto.Quantity)
//                    return BadRequest($"Not enough stock for product {product.Name}.");

//                product.Stock -= itemDto.Quantity;
//                product.Status = product.Stock > 0 ? "Available" : "Sold Out";

//                orderItems.Add(new OrderItem
//                {
//                    ProductID = itemDto.ProductID,
//                    Quantity = itemDto.Quantity,
//                    SubTotal = itemDto.Price * itemDto.Quantity,
//                    Color = itemDto.Color,      // ✅ REQUIRED
//                    Size = itemDto.Size

//                });
//            }

//            // Set all required fields for Orders
//            var order = new Orders
//            {
//                Total = (decimal)orderDto.Total,
//                OrderDate = DateTime.UtcNow,
//                PaymentMethod = orderDto.PaymentMethod, // ✅ Set properly
//                PlaceOrderViewID = customer.PlaceOrderViewID,
//                UserID = orderDto.CartItems.FirstOrDefault()?.UserID ?? 0, // ✅ Pull from cart
//                ShippingAddress = $"{customer.Address}, {customer.City}, {customer.PostalCode}", // ✅ Set full shipping

//                OrderItems = orderItems
//            };

//            _context.Orders.Add(order);
//            await _context.SaveChangesAsync();

//            return Ok(new
//            {
//                message = "Order placed successfully!",
//                orderId = order.OrderID
//            });
//        }



//        // GetOrders API Endpoint
//        [HttpGet("GetAllOrders")]
//        public IActionResult GetAllOrders()
//        {
//            var orders = _context.Orders
//                .AsQueryable()
//                .Include(o => o.PlaceOrderView)
//                .Select(o => new
//                {
//                    OrderID = o.OrderID,
//                    CustomerName = o.PlaceOrderView.FirstName + " " + o.PlaceOrderView.LastName,
//                    MobileNumber = o.PlaceOrderView.Phone,
//                    Total = (decimal)o.Total, // 👈 fix: cast decimal to double
//                    OrderDate = o.OrderDate,
//                    Status = o.Status    // ← include it
//                })
//                .ToList();

//            return Ok(orders);
//        }

//        //}
//        [HttpPut("UpdateStatus/{OrderID}")]
//        public async Task<IActionResult> UpdateStatus(int OrderID, [FromQuery] string status)
//        {
//            var order = await _context.Orders.FindAsync(OrderID);
//            if (order == null)
//                return NotFound();

//            // validate status
//            if (status != "Pending" && status != "Processing" && status != "Delivered")
//                return BadRequest("Invalid status");

//            order.Status = status;
//            await _context.SaveChangesAsync();
//            return Ok(new { OrderID, status });
//        }



//        // DELETE: api/checkout/DeleteOrder/{id}
//        [HttpDelete("DeleteOrder/{id}")]
//        public async Task<IActionResult> DeleteOrder(int id)
//        {
//            var order = await _context.Orders
//                                      .Include(o => o.OrderItems)  // ensure related items loaded
//                                      .FirstOrDefaultAsync(o => o.OrderID == id);
//            if (order == null)
//                return NotFound(new { message = $"Order with ID {id} not found." });

//            _context.OrderItem.RemoveRange(order.OrderItems);
//            _context.Orders.Remove(order);
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }
//        // GET api/checkout/GetOrderDetails/{orderId}
//        [HttpGet("GetOrderDetails/{orderId}")]
//        public async Task<IActionResult> GetOrderDetails(int orderId)
//        {
//            var order = await _context.Orders
//                .Include(o => o.PlaceOrderView)
//                .Where(o => o.OrderID == orderId)
//                .Select(o => new
//                {
//                    Email = o.PlaceOrderView.Email,
//                    CustomerName = o.PlaceOrderView.FirstName + " " + o.PlaceOrderView.LastName,
//                    OrderDate = o.OrderDate,
//                    Total = o.Total
//                })
//                .FirstOrDefaultAsync();

//            if (order == null) return NotFound();

//            return Ok(order);
//        }

//    }

//}using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DataAccess;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    
    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly DACheckout _checkout;

        public CheckoutController(DACheckout checkout)
        {
            _checkout = checkout;
        }

        [HttpPost("PlaceOrder")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderViewModel orderDto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid order data");

            int customerId = await _checkout.SaveCustomerAsync(orderDto);

            var orderItems = new List<OrderItemModel>();
            foreach (var itemDto in orderDto.CartItems)
            {
                bool updated = await _checkout.UpdateProductStock(itemDto.ProductID, itemDto.Quantity);
                if (!updated)
                    return BadRequest($"Product {itemDto.ProductID} not found or out of stock.");

                orderItems.Add(new OrderItemModel
                {
                    ProductID = itemDto.ProductID,
                    Quantity = itemDto.Quantity,
                    SubTotal = itemDto.Price * itemDto.Quantity,
                    Color = itemDto.Color,
                    Size = itemDto.Size
                });
            }

            var order = new OrderModel
            {
                Total = (decimal)orderDto.Total,
                OrderDate = DateTime.UtcNow,
                PaymentMethod = orderDto.PaymentMethod,
                PlaceOrderViewID = customerId,
                UserID = orderDto.CartItems.FirstOrDefault()?.UserID ?? 0,
                ShippingAddress = $"{orderDto.Address}, {orderDto.City}, {orderDto.PostalCode}"
            };

            int orderId = await _checkout.PlaceOrderAsync(order);
            await _checkout.SaveOrderItemsAsync(orderItems, orderId);

            return Ok(new { message = "Order placed successfully!", orderId });
        }

        //[HttpGet("GetAllOrders")]
        //public IActionResult GetAllOrders()
        //{
        //    var orders = _checkout.GetAllOrders();
        //    return Ok(orders);
        //}

        [HttpPut("UpdateStatus/{OrderID}")]
        public async Task<IActionResult> UpdateStatus(int OrderID, [FromQuery] string status)
        {
            if (status != "Pending" && status != "Processing" && status != "Delivered" && status != "Returned")
                return BadRequest("Invalid status");

            bool updated = await _checkout.UpdateOrderStatus(OrderID, status);
            return updated ? Ok(new { OrderID, status }) : NotFound();
        }

        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _checkout.GetAllOrdersAsync(); 
                return Ok(orders);
            }
            catch (Exception ex)
            {
                // Show full error for debugging (TEMPORARY - remove in production)
                return StatusCode(500, $"🔥 ERROR: {ex.Message} \n\n STACKTRACE: {ex.StackTrace}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok("pong from CheckoutController");



        [HttpDelete("DeleteOrder/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            bool deleted = await _checkout.DeleteOrderAsync(id);
            return deleted ? NoContent() : NotFound(new { message = $"Order with ID {id} not found." });
        }

        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var order = await _checkout.GetOrderDetailsAsync(orderId);
            return order == null ? NotFound() : Ok(order);
        }
    }
}



