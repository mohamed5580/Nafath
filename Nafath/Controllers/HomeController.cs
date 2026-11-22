using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Models;
using Domin.Entity;
using Infrastructure.IRepository.Base;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepository<ProductType> _productTypeRepo;
        private readonly IRepository<Product> _productRepo;

        public HomeController(
            ILogger<HomeController> logger,
            IRepository<ProductType> productTypeRepo,
            IRepository<Product> productRepo)
        {
            _logger = logger;
            _productTypeRepo = productTypeRepo;
            _productRepo = productRepo;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _productTypeRepo.FindAllasync();
            var featuredProducts = await _productRepo.Query("Images", "ProductType", "Colors")
                .Where(product => product.IsAvailable && product.StockQuantity > 0)
                .OrderByDescending(product => product.Id)
                .Take(8)
                .ToListAsync();

            return View(new HomePageViewModel
            {
                Categories = categories.OrderBy(category => category.Name).ToList(),
                FeaturedProducts = featuredProducts
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Services()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
