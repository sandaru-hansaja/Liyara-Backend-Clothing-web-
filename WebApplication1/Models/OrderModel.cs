using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class OrderModel
    {
        [Key]
        public int OrderID { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }

        // Foreign key to user
        public int UserID { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; } = null!;

        // FK → PlaceOrder
        public int PlaceOrderViewID { get; set; }
        //public PlaceOrderView PlaceOrderView { get; set; } = null!;


        // Navigation property
        public ICollection<OrderItemModel> OrderItems { get; set; }
    }
}
