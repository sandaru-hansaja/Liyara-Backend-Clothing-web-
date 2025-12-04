namespace WebApplication1.Models
{
    public class ProductReviewModel
    {
        public int ReviewID { get; set; }
        public int ProductID { get; set; }
        public string UserName { get; set; } = "";
        public string ReviewText { get; set; } = "";
        public DateTime ReviewDate { get; set; }
        public int Likes { get; set; }
        public int Dislikes { get; set; }
        public int Rating { get; set; }

    }
}
