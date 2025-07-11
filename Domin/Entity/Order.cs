using System.ComponentModel.DataAnnotations;


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

    }

}
