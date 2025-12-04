using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class CategoryModel
    {
        [Key]
        public int CategoryID { get; set; } // Primary Key

        public string? CategoryName { get; set; }

        public string? States { get; set; }  // "A" = Active, "I" = Inactive

        // List of related subcategories
        public ICollection<SubCategoryModel> SubCategory { get; set; } = new List<SubCategoryModel>();
    }
}
