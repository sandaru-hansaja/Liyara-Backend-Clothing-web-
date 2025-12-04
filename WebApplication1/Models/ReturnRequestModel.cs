    namespace WebApplication1.Models
{
    public class ReturnRequestModel
    {
        public int ReturnID { get; set; }
        public int OrderID { get; set; }
        //public int ProductID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Reason { get; set; }
        public string ProductCondition { get; set; }
        public string? Comment { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
