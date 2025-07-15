using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;


namespace Domin.Entity
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // FK to user
        [Required]
        public string UserId { get; set; }

        // يجب إضافة هذا السطر:
        public List<OrderItem> OrderItems { get; set; } = new();

        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "قيد المراجعة"; // Default status

    }

}
