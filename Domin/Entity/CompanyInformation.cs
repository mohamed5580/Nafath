using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public class CompanyInformation
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = "????";

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; } = "??? ??? ????? 6 ??????";

        [StringLength(1000)]
        public string? MapUrl { get; set; }

        [Required, StringLength(50)]
        public string PhoneNumber { get; set; } = "01126499600";

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = "info@nafath.com";

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? FacebookUrl { get; set; }

        [StringLength(500)]
        public string? InstagramUrl { get; set; }

        [StringLength(500)]
        public string? WebsiteUrl { get; set; }

        [StringLength(500)]
        public string? OtherInformation { get; set; }

        [Range(0, int.MaxValue)]
        public int ClientsNumber { get; set; }

        [Range(0, int.MaxValue)]
        public int ProjectsNumber { get; set; }

        [Range(0, int.MaxValue)]
        public int SupportHours { get; set; }

        [Range(0, int.MaxValue)]
        public int EmployeesNumber { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
