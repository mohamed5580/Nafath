using Domin.Entity;
using Infrastructure.IRepository.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductTypeManagerController : Controller
    {
        #region Declaration
        private readonly IRepository<ProductType> _context;
        private readonly IWebHostEnvironment _env;
        #endregion

        #region Constructor
        public ProductTypeManagerController(IRepository<ProductType> context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        #endregion

        #region Method
        public async Task<IActionResult> ByType(int id)
        {
            var types = await _context.FindAllasync("Products");
            var type = types.FirstOrDefault(t => t.Id == id);
            if (type == null) return NotFound();
            
            return View(type);
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 1000)
        {
            var (items, total) = await _context.GetPagedAsync(page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ProductType = await _context.FindByIdasync(id.Value);
            if (ProductType == null) return NotFound();

            return View(ProductType);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductType ProductType)
        {
            if (!ModelState.IsValid)
            {
                int totalItems;
                var allProductType = _context.GetPaged(1, 1000, out totalItems).ToList();

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.EditErrors = errors;
                SessionMsg(Helper.Error, "خطأ", string.Join(" | ", errors));

                return View("Index", allProductType);
            }

            _context.AddOne(ProductType);
            SessionMsg(Helper.Success, "تم", "تم إضافة النوع بنجاح");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var chair = _context.FindById(id.Value);
            if (chair == null) return NotFound();

            return View(chair);
        }

        public async Task<IActionResult> Edit(int id, ProductType ProductType, IFormFile? ImageFile)
        {
            if (id != ProductType.Id) return NotFound();

            var existing = _context.FindById(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                int totalItems;
                var allProductType = _context.GetPaged(1, 1000, out totalItems).ToList();

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.EditErrors = errors;
                SessionMsg(Helper.Error, "خطأ", string.Join(" | ", errors));

                return View("Index", allProductType);
            }

            existing.Name = ProductType.Name;

            try
            {
                _context.UpdateOne(existing);
                SessionMsg(Helper.Success, "تم", "تم تحديث النوع بنجاح");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.FindById(id) == null) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ProductType = await _context.FindByIdasync(id.Value);
            if (ProductType == null) return NotFound();

            return View(ProductType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null) return NotFound();

            var chair = await _context.FindByIdasync(id.Value);
            if (chair != null)
            {
                _context.DeleteOne(chair);
            }

            SessionMsg(Helper.Success, "تم", "تم حذف النوع بنجاح");
            return RedirectToAction(nameof(Index));
        }

        private bool ProductTypeExists(int id)
        {
            return _context.FindById(id) != null;
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
        #endregion
    }
}
