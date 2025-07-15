using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class CheckoutViewModel
    {
        [Required]
        [MaxLength(30)]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(30)]
        public string? Email { get; set; }
        [Required]
        public string? MobileNumber { get; set; }
        [Required]
        [MaxLength(200)]
        public string? Address { get; set; }

        [Required]
        public string? PaymentMethod { get; set; }

        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        [Required]
        [StringLength(500, ErrorMessage = "OrderStatus can be at most 500 characters.")]
        public string OrderStatus { get; set; }
    }
}
