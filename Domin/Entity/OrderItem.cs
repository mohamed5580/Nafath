using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        // FK to Order
        [Required]
        public int OrderId { get; set; }

        public Order Order { get; set; }

        // FK to Chair
        [Required]
        public int ChairId { get; set; }

        public Chairs Chair { get; set; }

        // عدد الكراسي من هذا النوع في الطلب
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // يمكنك أيضاً إضافة سعر في لحظة الطلب لتاريخه
        [Required]
        public decimal UnitPrice { get; set; }
    }
}