using Domin.Entity;
using Domin.Resource;
using Infrastructure.IRepository.Base;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Controllers
{
    public class ProductsController : Controller
    {
        #region Declaration
        private readonly IRepository<Product> _productRepo;
        private readonly IRepository<ProductType> _typeRepo;
        private readonly IWebHostEnvironment _env;
        #endregion

        #region Constructor
        public ProductsController(
            IRepository<Product> productRepo,
            IRepository<ProductType> typeRepo,
            IWebHostEnvironment env)
        {
            _productRepo = productRepo;
            _typeRepo = typeRepo;
            _env = env;
        }
        #endregion

        #region Actions

        // GET: Products
        public async Task<IActionResult> Index([FromQuery] ProductFilterVm filter)
        {
            filter ??= new ProductFilterVm();
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

            var baseQuery = _productRepo.Query("ProductType", "Images", "Colors");

            // Calculate overall price range for filter UI
            var allPrices = await _productRepo.Query()
                .Where(p => p.Price.HasValue)
                .Select(p => p.Price!.Value)
                .ToListAsync();

            filter.PriceFloor = allPrices.Any() ? Math.Floor(allPrices.Min()) : 0;
            filter.PriceCeiling = allPrices.Any() ? Math.Ceiling(allPrices.Max()) : 10000;

            // Apply Filters
            var query = baseQuery;

            // 1) Search keyword (Name, Description, SKU)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(term)));
            }

            // 2) Category / ProductType filter
            if (filter.ProductTypeId.HasValue && filter.ProductTypeId.Value > 0)
            {
                query = query.Where(p => p.ProductTypeId == filter.ProductTypeId.Value);
            }

            // 3) Price range filter
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            // 4) In-Stock only filter
            if (filter.InStockOnly)
            {
                query = query.Where(p => p.IsAvailable && p.StockQuantity > 0);
            }

            // 5) Sorting
            query = (filter.SortBy?.ToLower()) switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "oldest" => query.OrderBy(p => p.Id),
                _ => query.OrderByDescending(p => p.Id), // default: newest
            };

            // 6) Pagination
            filter.TotalItems = await query.CountAsync();
            filter.Products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // 7) Categories list for sidebar / pills
            filter.Categories = (await _typeRepo.FindAllasync()).OrderBy(c => c.Name).ToList();

            // Backward compatibility for existing views/helpers
            ViewBag.CurrentPage = filter.Page;
            ViewBag.TotalPages = filter.TotalPages;
            ViewBag.TypeFilterId = filter.ProductTypeId;
            ViewBag.ProductTypes = filter.Categories;

            return View(filter);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productRepo.Query("ProductType", "Images", "Colors")
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product == null) return NotFound();

            // Fetch related products in same category
            var relatedProducts = await _productRepo.Query("ProductType", "Images")
                .Where(p => p.ProductTypeId == product.ProductTypeId && p.Id != product.Id && p.IsAvailable)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // GET: Products/ByType?typeId=1
        public IActionResult ByType(int typeId)
        {
            return RedirectToAction(nameof(Index), new { ProductTypeId = typeId });
        }

        #endregion
    }
}
