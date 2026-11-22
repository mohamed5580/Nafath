using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nafath.Services;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CompanyInformationController : Controller
    {
        private readonly ICompanyInformationService _companyService;
        private readonly IWebHostEnvironment _environment;

        public CompanyInformationController(
            ICompanyInformationService companyService,
            IWebHostEnvironment environment)
        {
            _companyService = companyService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var company = await _companyService.GetAsync();
            return View(ToViewModel(company));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CompanyInformationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.LogoFile != null)
            {
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".webp", ".gif"
                };

                var extension = Path.GetExtension(model.LogoFile.FileName);
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.LogoFile), "???? ??? ???? JPG ?? PNG ?? WEBP ?? GIF.");
                    return View(model);
                }

                if (model.LogoFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), "??? ??? ???? ??? ?????? ?? 2 ????????.");
                    return View(model);
                }

                var uploadsDirectory = Path.Combine(_environment.WebRootPath, "uploads", "company");
                Directory.CreateDirectory(uploadsDirectory);

                var fileName = $"{Guid.NewGuid():N}{extension}";
                var physicalPath = Path.Combine(uploadsDirectory, fileName);
                await using (var stream = System.IO.File.Create(physicalPath))
                {
                    await model.LogoFile.CopyToAsync(stream);
                }

                var oldLogoUrl = model.ExistingLogoUrl;
                model.ExistingLogoUrl = $"/uploads/company/{fileName}";

                if (!string.IsNullOrWhiteSpace(oldLogoUrl) && oldLogoUrl.StartsWith("/uploads/"))
                {
                    var oldPhysicalPath = Path.Combine(_environment.WebRootPath, oldLogoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }
            }

            await _companyService.SaveAsync(new CompanyInformation
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                LogoUrl = model.ExistingLogoUrl,
                Address = model.Address.Trim(),
                MapUrl = model.MapUrl?.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Email = model.Email.Trim(),
                Description = model.Description?.Trim(),
                FacebookUrl = model.FacebookUrl?.Trim(),
                InstagramUrl = model.InstagramUrl?.Trim(),
                WebsiteUrl = model.WebsiteUrl?.Trim(),
                OtherInformation = model.OtherInformation?.Trim(),
                ClientsNumber = model.ClientsNumber,
                ProjectsNumber = model.ProjectsNumber,
                SupportHours = model.SupportHours,
                EmployeesNumber = model.EmployeesNumber
            });

            TempData["Success"] = "تم التعديل بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private static CompanyInformationViewModel ToViewModel(CompanyInformation company)
        {
            return new CompanyInformationViewModel
            {
                Id = company.Id,
                Name = company.Name,
                ExistingLogoUrl = company.LogoUrl,
                Address = company.Address,
                MapUrl = company.MapUrl,
                PhoneNumber = company.PhoneNumber,
                Email = company.Email,
                Description = company.Description,
                FacebookUrl = company.FacebookUrl,
                InstagramUrl = company.InstagramUrl,
                WebsiteUrl = company.WebsiteUrl,
                OtherInformation = company.OtherInformation,
                ClientsNumber = company.ClientsNumber,
                ProjectsNumber = company.ProjectsNumber,
                SupportHours = company.SupportHours,
                EmployeesNumber = company.EmployeesNumber
            };
        }
    }
}
