using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class PlaceOrderViewModel
    {
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required] public string Address { get; set; }
        [Required] public string City { get; set; }
        [Required] public string PostalCode { get; set; }
        public string Phone { get; set; }
        [Required, EmailAddress] public string Email { get; set; }

        // Payment method – e.g. "cod", "card", etc.
        [Required] public string PaymentMethod { get; set; }

        [Required] public decimal Total { get; set; }

        public List<CartItemModel> CartItems { get; set; }
    }
}
