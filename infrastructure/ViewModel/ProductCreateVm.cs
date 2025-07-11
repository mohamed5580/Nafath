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

        public bool IsAvailable { get; set; } = true;

        public IFormFile? ImageFile { get; set; }
    }
}
