using Infrastructure.ViewModel;

namespace Nafath.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetStatisticsAsync();
    }
}
