using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domin.Entity
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name can be at most 100 characters.")]
        public string Name { get; set; }

        [Required, StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
        public string Description { get; set; }

        [NotMapped]
        [Display(Name = "Upload Image")]
        public IFormFile ImageFile { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 100000, ErrorMessage = "Price must be positive.")]
        public decimal? Price { get; set; }

        [Display(Name = "Old Price")]
        [Range(0.01, 100000, ErrorMessage = "Old price must be positive.")]
        public decimal? OldPrice { get; set; }

        [StringLength(50)]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, 100000, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; } = 10;

        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 3;

        /// <summary>Comma-separated sizes enabled for this product (for example: XXS,XS,S,M,L,XL,XXL).</summary>
        [StringLength(100)]
        public string? AvailableSizes { get; set; }

        [NotMapped]
        public bool IsOnSale => OldPrice.HasValue && OldPrice > Price;

        [NotMapped]
        public bool IsInStock => IsAvailable && StockQuantity > 0;

        [Required]
        public int ProductTypeId { get; set; }

        public ProductType? ProductType { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();

        [NotMapped]
        public IReadOnlyList<string> Sizes => ProductSizeOptions.Parse(AvailableSizes);
    }

    public static class ProductSizeOptions
    {
        public static readonly string[] All = ["XXS", "XS", "S", "M", "L", "XL", "XXL", "3XL", "Free Size"];

        public static IReadOnlyList<string> Parse(string? sizes) =>
            string.IsNullOrWhiteSpace(sizes)
                ? []
                : sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(size => size.Trim())
                    .Where(size => !string.IsNullOrEmpty(size))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        public static string? Serialize(IEnumerable<string>? sizes)
        {
            var selected = (sizes ?? [])
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(size =>
                {
                    int idx = Array.FindIndex(All, s => s.Equals(size, StringComparison.OrdinalIgnoreCase));
                    return idx >= 0 ? idx : 999;
                })
                .ToArray();

            return selected.Length == 0 ? null : string.Join(',', selected);
        }
    }
}
