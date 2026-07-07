using Domin.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.ViewModel
{
    public class RegisterViewModel
    {
        [BindNever]
        public List<IdentityRole> Roles { get; set; } = new();

        [BindNever]
        public List<VwUsers> Users { get; set; } = new();

        public NewRegister NewRegister { get; set; } = new();

        [BindNever]
        [ValidateNever]
        public ChangePasswordViewModel ChangePassword { get; set; } = new();
    }

    public class NewRegister
    {
        public string? Id { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string? FullName { get; set; }

        [Required]
        public string? RoleName { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        // this is the **upload**:
        public IFormFile? ImageFile { get; set; }

        // this is the saved file name:
        public string? ImageUser { get; set; }

        // **must** be non‑nullable bool for asp-for="ActiveUser"
        public bool ActiveUser { get; set; } = true;

        [MinLength(5), MaxLength(20)]
        public string? Password { get; set; }

        [Compare(nameof(Password))]
        public string? ComparePassword { get; set; }
    }

   
}
