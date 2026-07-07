using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
      
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.OrderItems.SumAsync(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0;
            var totalUsers = await _userManager.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o =>
                o.OrderStatus == "قيد المراجعة" || o.OrderStatus == "قيد الشحن");
            var completedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "مكتمل");

            var monthlyData = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= sixMonthsAgo)
                .GroupBy(oi => new { oi.Order.OrderDate.Year, oi.Order.OrderDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToListAsync();

            var monthlyLabels = new List<string>();
            var monthlyRevenue = new List<decimal>();
            for (var i = 0; i < 6; i++)
            {
                var month = sixMonthsAgo.AddMonths(i);
                monthlyLabels.Add(month.ToString("MMM yyyy"));
                var data = monthlyData.FirstOrDefault(m => m.Year == month.Year && m.Month == month.Month);
                monthlyRevenue.Add(data?.Revenue ?? 0);
            }

            var statusData = await _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                MonthlyLabels = monthlyLabels,
                MonthlyRevenue = monthlyRevenue,
                OrderStatusLabels = statusData.Select(s => s.Status).ToList(),
                OrderStatusCounts = statusData.Select(s => s.Count).ToList()
            };

            return View(model);
        }
    }
}
