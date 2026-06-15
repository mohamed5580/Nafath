// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Mono.TextTemplating;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Infrastructure.Models;
using Domin.Entity;

namespace Nafath.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        // Removed ReturnUrl, as we redirect to the same page.

        public class InputModel
        {
            [Display(Name = "Current Avatar")]
            public string? AvatarUrl { get; set; }

            [Display(Name = "Change Avatar")]
            [DataType(DataType.Upload)]
            public IFormFile? AvatarFile { get; set; }

            [Required, MaxLength(100)]
            [Display(Name = "First Name")]
            public string FullName { get; set; } = string.Empty;

            [Required, MaxLength(100)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "User Name")]
            public string? UserName { get; set; }

            // Note: Password fields are now optional at the model level
            // because the profile form doesn't need them. We will validate them manually in the password handler.
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string? CurrentPassword { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation do not match.")]
            public string? ConfirmPassword { get; set; }

            [Required, Phone]
            [Display(Name = "Phone No.")]
            public string PhoneNumber { get; set; }

            [MaxLength(50)][Display(Name = "City")] public string? City { get; set; }
            [Required][Display(Name = "Gender")] public string? Gender { get; set; }
            [DataType(DataType.Date)][Display(Name = "Date of Birth")] public DateTime? DateOfBirth { get; set; }
            [MaxLength(50)][Display(Name = "Marital Status")] public string? MaritalStatus { get; set; }
            [MaxLength(20)][Display(Name = "Age Range")] public string? AgeRange { get; set; }
            [MaxLength(50)][Display(Name = "Country")] public string? Country { get; set; }
            [MaxLength(50)][Display(Name = "State")] public string? State { get; set; }
            [MaxLength(500), DataType(DataType.MultilineText)]
            [Display(Name = "Address")]
            public string? Address { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Input = new InputModel
            {
                AvatarUrl = user.AvatarUrl,
                FullName = user.FullName,
                LastName = user.LastName,
                UserName = user.UserName,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                AgeRange = user.AgeRange,
                Address = user.Address,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            // Use a more specific TempData key to avoid conflicts
            if (!string.IsNullOrEmpty(TempData["ProfileStatusMessage"]?.ToString()))
                StatusMessage = TempData["ProfileStatusMessage"].ToString();

            await LoadAsync(user);
            return Page();
        }

        // HANDLER 1: For updating the user's profile information
        public async Task<IActionResult> OnPostProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            // We only validate the profile part of the model here
            // We can do this by removing password fields from ModelState if needed, but making them nullable is easier.
            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // --- Avatar Upload Logic ---
            if (Input.AvatarFile != null)
            {
                // 1. Define a path to save the file
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/avatars");
                Directory.CreateDirectory(uploadsFolder); // Ensure the folder exists

                // 2. Create a unique filename to avoid conflicts
                string uniqueFileName = user.Id + "_" + Guid.NewGuid().ToString() + Path.GetExtension(Input.AvatarFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(fileStream);
                }

                // 4. Update the user's AvatarUrl property with the web-accessible path
                user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
            }

            // --- Phone update ---
            var phone = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phone)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Error: Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // --- Update other profile fields ---
            user.FullName = Input.FullName;
            user.LastName = Input.LastName;
            user.Gender = Input.Gender;
            user.DateOfBirth = Input.DateOfBirth;
            user.AgeRange = Input.AgeRange;
            user.Address = Input.Address;

            // Note: UserName is usually not changed this way, but preserving your logic
            if (!string.IsNullOrWhiteSpace(Input.UserName) && Input.UserName != user.UserName)
                user.UserName = Input.UserName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["ProfileStatusMessage"] = "Your profile has been updated successfully!";
            return RedirectToPage();
        }

        // HANDLER 2: For changing the user's password
        public async Task<IActionResult> OnPostPasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            // Manual validation for password fields
            if (string.IsNullOrEmpty(Input.CurrentPassword) || string.IsNullOrEmpty(Input.NewPassword) || string.IsNullOrEmpty(Input.ConfirmPassword))
            {
                ModelState.AddModelError("", "All password fields are required to change the password.");
                await LoadAsync(user);
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["ProfileStatusMessage"] = "Your password has been changed successfully!";
            return RedirectToPage();
        }
    }
}