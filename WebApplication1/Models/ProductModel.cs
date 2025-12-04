namespace WebApplication1.Models
{
    public class ProductModel
    {
        public int ProductID { get; set; }
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public List<IFormFile> Images { get; set; } = new();
        public List<string> Colors { get; set; } = new();
        public List<string>? ImageUrls { get; set; } = new List<string>();
        public List<string> Sizes { get; set; } = new();
        public string? Description { get; set; }
    }
}
