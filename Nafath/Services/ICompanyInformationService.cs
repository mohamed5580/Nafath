using Domin.Entity;

namespace Nafath.Services
{
    public interface ICompanyInformationService
    {
        Task<CompanyInformation> GetAsync();
        Task SaveAsync(CompanyInformation information);
    }
}
