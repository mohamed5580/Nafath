using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domin.Entity
{
    public class ProductColor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "اسم اللون مطلوب.")]
        [StringLength(50, ErrorMessage = "اسم اللون لا يتجاوز 50 حرفاً.")]
        [Display(Name = "اسم اللون")]
        public string ColorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كود اللون (HEX) مطلوب.")]
        [StringLength(7, MinimumLength = 4, ErrorMessage = "كود اللون يجب أن يكون بصيغة HEX صحيحة.")]
        [RegularExpression(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", ErrorMessage = "كود اللون يجب أن يكون بصيغة HEX صحيحة (مثال: #FF0000).")]
        [Display(Name = "كود اللون (HEX)")]
        public string HexCode { get; set; } = string.Empty;

        [Display(Name = "ترتيب العرض")]
        public int DisplayOrder { get; set; } = 0;
    }
}
