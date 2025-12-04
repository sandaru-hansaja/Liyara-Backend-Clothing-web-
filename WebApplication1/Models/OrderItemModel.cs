using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class OrderItemModel
    {
        [Key]
        public int OrderItemID { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
        // Foreign key to Orders
        public int OrderID { get; set; }
        public OrderModel Order { get; set; }
        public decimal Price { get; set; }


        // Foreign key to Product
        public int ProductID { get; set; }
        //public Product Product { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }

    }
}
