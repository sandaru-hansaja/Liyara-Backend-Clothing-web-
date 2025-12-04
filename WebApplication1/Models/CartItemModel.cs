namespace WebApplication1.Models
{
    public class CartItemModel
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int Id { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public string? ImageData { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public UserModel User { get; set; }
    }
}
