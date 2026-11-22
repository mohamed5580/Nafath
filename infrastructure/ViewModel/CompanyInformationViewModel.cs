using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.ViewModel
{
    public class CompanyInformationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم غير موجود ")]
        [StringLength(150)]
        [Display(Name = "اسم الشركة")]
        public string Name { get; set; } = "نفاذ";

        [Display(Name = "الصورة")]
        public IFormFile? LogoFile { get; set; }

        public string? ExistingLogoUrl { get; set; }

        [Required(ErrorMessage = "اكتب عنوان الشركة")]
        [StringLength(300)]
        [Display(Name = "عنوان الشركة")]
        public string Address { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "رابط Google Maps")]
        public string? MapUrl { get; set; }

        [Required(ErrorMessage = "أدخل رقم الهاتف")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(50)]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "أدخل البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [StringLength(150)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "رابط فيسبوك غير صحيح")]
        [StringLength(500)]
        [Display(Name = "رابط فيسبوك")]
        public string? FacebookUrl { get; set; }

        [Url(ErrorMessage = "رابط إنستغرام غير صحيح")]
        [StringLength(500)]
        [Display(Name = "رابط إنستغرام")]
        public string? InstagramUrl { get; set; }

        [Url(ErrorMessage = "رابط الموقع الإلكتروني غير صحيح")]
        [StringLength(500)]
        [Display(Name = "رابط الموقع الإلكتروني")]
        public string? WebsiteUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "معلومات إضافية")]
        public string? OtherInformation { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "عدد العملاء")]
        public int ClientsNumber { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "عدد المشاريع")]
        public int ProjectsNumber { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "ساعات الدعم")]
        public int SupportHours { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "عدد الموظفين")]
        public int EmployeesNumber { get; set; }
    }
}
