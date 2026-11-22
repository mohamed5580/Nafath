using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class ProductEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name can be at most 100 characters.")]
        public string Name { get; set; }

        [Required, StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
        public string Description { get; set; }

        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

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

        [Display(Name = "Gallery Images to Add")]
        public List<IFormFile>? AdditionalImages { get; set; }

        public List<Domin.Entity.ProductImage>? ExistingImages { get; set; }

        [Required]
        public int ProductTypeId { get; set; }

        /// <summary>Existing colors loaded from DB for rendering in Edit page.</summary>
        public List<Domin.Entity.ProductColor> ExistingColors { get; set; } = new();

        /// <summary>Colors submitted as JSON from the color-picker UI.</summary>
        public string? ColorsJson { get; set; }
    }
}

