using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }      // مفرد وليس Orders

       // مفرد وليس Products

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

}