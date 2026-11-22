using System.ComponentModel.DataAnnotations;

namespace Infrastructure.ViewModel
{
    /// <summary>
    /// DTO used to transfer a single color entry from the form (Create / Edit).
    /// </summary>
    public class ProductColorDto
    {
        public int Id { get; set; }   // 0 for new, > 0 for existing

        [Required(ErrorMessage = "اسم اللون مطلوب.")]
        [StringLength(50)]
        public string ColorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كود اللون مطلوب.")]
        [StringLength(7, MinimumLength = 4)]
        public string HexCode { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
