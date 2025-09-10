using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public class ProductType
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public ICollection<Product>? Products { get; set; } = new List<Product>();
    }
}