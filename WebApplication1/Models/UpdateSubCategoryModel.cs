using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class UpdateSubCategoryModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<string> SubCategoryNames { get; set; } = new();
        public string States { get; set; }  // "A" or "I"


    }
}
