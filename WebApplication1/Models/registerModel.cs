using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class registerModel
    {
        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        //public string? Role { get; set; }
    }
}
