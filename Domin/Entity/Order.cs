using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public partial class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        // ← requires System.Collections.Generic
        public List<OrderItem> OrderItems { get; set; } = new();

        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "قيد المراجعة" ;

        [Required(ErrorMessage = "يرجى اختيار طريقة الدفع")]
        [StringLength(200)]
        public string? PaymentMethod { get; set; }

        [Required(ErrorMessage = "يرجى ادخال العنوان")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        public string? MobileNumber { get; set; }

    }
}