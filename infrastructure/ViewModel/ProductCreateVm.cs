using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.ViewModel
{
    public class ProductCreateVm
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int ProductTypeId { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; }

        [Required, Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Range(0.01, 100000)]
        public decimal? OldPrice { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [Range(0, 100000)]
        public int StockQuantity { get; set; } = 10;

        public int LowStockThreshold { get; set; } = 3;

        public List<string> Sizes { get; set; } = new();

        public string? CustomSizes { get; set; }

        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Main Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Gallery Images")]
        public List<IFormFile>? AdditionalImages { get; set; }

        /// <summary>Colors submitted as JSON from the color-picker UI.</summary>
        public string? ColorsJson { get; set; }
    }
}
