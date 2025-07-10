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

        // مجموعة العناصر داخل الطلب
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

}
