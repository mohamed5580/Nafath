using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Services
{
    public class DashboardService : IDashboardService
    {
        private const int ChartMonths = 6;
        private const int RecentOrdersLimit = 10;
        private const int TopProductsLimit = 5;

        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetStatisticsAsync()
        {
            var now = DateTime.Now;
            var firstChartMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(ChartMonths - 1));

            var statusCounts = await _context.Orders
                .AsNoTracking()
                .GroupBy(order => order.OrderStatus)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync();

            var totalUsers = await _context.Users.AsNoTracking().CountAsync();
            var totalProducts = await _context.Products.AsNoTracking().CountAsync();

            var completedOrderItems = _context.OrderItems
                .AsNoTracking()
                .Where(orderItem => orderItem.Order.OrderStatus == OrderStatuses.Completed);

            var totalRevenue = await completedOrderItems
                .SumAsync(orderItem => (decimal?)(orderItem.Quantity * orderItem.UnitPrice)) ?? 0;

            var monthlyData = await completedOrderItems
                .Where(orderItem => orderItem.Order.OrderDate >= firstChartMonth)
                .GroupBy(orderItem => new
                {
                    orderItem.Order.OrderDate.Year,
                    orderItem.Order.OrderDate.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Revenue = group.Sum(orderItem => orderItem.Quantity * orderItem.UnitPrice)
                })
                .ToListAsync();

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.User)
                .Include(order => order.OrderItems)
                .OrderByDescending(order => order.OrderDate)
                .Take(RecentOrdersLimit)
                .ToListAsync();

            var topProducts = await completedOrderItems
                .GroupBy(orderItem => new
                {
                    orderItem.ProductId,
                    orderItem.Product.Name
                })
                .Select(group => new TopProductDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.Name,
                    QuantitySold = group.Sum(orderItem => orderItem.Quantity),
                    Revenue = group.Sum(orderItem => orderItem.Quantity * orderItem.UnitPrice)
                })
                .OrderByDescending(product => product.QuantitySold)
                .Take(TopProductsLimit)
                .ToListAsync();

            var monthlyLabels = new List<string>();
            var monthlyRevenue = new List<decimal>();

            for (var i = 0; i < ChartMonths; i++)
            {
                var month = firstChartMonth.AddMonths(i);
                var data = monthlyData.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month);

                monthlyLabels.Add(month.ToString("MMM yyyy"));
                monthlyRevenue.Add(data?.Revenue ?? 0);
            }

            return new DashboardViewModel
            {
                TotalOrders = statusCounts.Sum(item => item.Count),
                TotalRevenue = totalRevenue,
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                PendingOrders = statusCounts
                    .Where(item => OrderStatuses.Pending.Contains(item.Status))
                    .Sum(item => item.Count),
                CompletedOrders = statusCounts
                    .Where(item => item.Status == OrderStatuses.Completed)
                    .Sum(item => item.Count),
                MonthlyLabels = monthlyLabels,
                MonthlyRevenue = monthlyRevenue,
                OrderStatusLabels = statusCounts
                    .Select(item => item.Status)
                    .ToList(),
                OrderStatusCounts = statusCounts
                    .Select(item => item.Count)
                    .ToList(),
                RecentOrders = recentOrders
                    .Select(order => new RecentOrderDto
                    {
                        Id = order.Id,
                        OrderDate = order.OrderDate,
                        CustomerName = GetCustomerName(order.User),
                        OrderStatus = order.OrderStatus,
                        Total = order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.UnitPrice)
                    })
                    .ToList(),
                TopProducts = topProducts
            };
        }

        private static string GetCustomerName(ApplicationUser? user)
        {
            if (user == null)
            {
                return "غير معروف";
            }

            var fullName = $"{user.FullName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName)
                ? user.UserName ?? user.Email ?? "غير معروف"
                : fullName;
        }
    }
}
