using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Services
{
    public class CompanyInformationService : ICompanyInformationService
    {
        private readonly ApplicationDbContext _db;

        public CompanyInformationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CompanyInformation> GetAsync()
        {
            var information = await _db.CompanyInformation.AsNoTracking().FirstOrDefaultAsync();

            if (information != null)
            {
                return information;
            }

            return new CompanyInformation
            {
                Name = "????",
                Address = "??? ??? ????? 6 ??????",
                PhoneNumber = "01126499600",
                Email = "info@nafath.com",
                LogoUrl = "/Nafas/assets/images/logoo.png",
                Description = "???? ???? ?????? ???? ?????? ?????? ???????.",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task SaveAsync(CompanyInformation information)
        {
            var existing = await _db.CompanyInformation.FirstOrDefaultAsync();

            if (existing == null)
            {
                information.UpdatedAt = DateTime.UtcNow;
                _db.CompanyInformation.Add(information);
            }
            else
            {
                existing.Name = information.Name;
                existing.LogoUrl = information.LogoUrl;
                existing.Address = information.Address;
                existing.MapUrl = information.MapUrl;
                existing.PhoneNumber = information.PhoneNumber;
                existing.Email = information.Email;
                existing.Description = information.Description;
                existing.FacebookUrl = information.FacebookUrl;
                existing.InstagramUrl = information.InstagramUrl;
                existing.WebsiteUrl = information.WebsiteUrl;
                existing.OtherInformation = information.OtherInformation;
                existing.ClientsNumber = information.ClientsNumber;
                existing.ProjectsNumber = information.ProjectsNumber;
                existing.SupportHours = information.SupportHours;
                existing.EmployeesNumber = information.EmployeesNumber;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }
}
