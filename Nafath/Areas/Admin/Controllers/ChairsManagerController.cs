using Domin.Entity;
using Infrastructure.IRepository.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChairsManagerController : Controller
    {
        #region Declaration
        private readonly IRepository<Chairs> _context;
        private readonly IWebHostEnvironment _env;
        #endregion

        #region Constructor
        public ChairsManagerController(IRepository<Chairs> context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        #endregion

        #region Method
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Index(int page = 1, int pageSize = 1000)
        {
            int totalItems;
            var pagedChairs = _context.GetPaged(page, pageSize, out totalItems)
                              .Where(c => c != null)
                              .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(pagedChairs);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var chairs = await _context.FindByIdasync(id.Value);
            if (chairs == null) return NotFound();

            return View(chairs);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chairs chairs)
        {
            if (!ModelState.IsValid)
            {
                int totalItems;
                var allChairs = _context.GetPaged(1, 1000, out totalItems).ToList();

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.EditErrors = errors;
                SessionMsg(Helper.Error, "خطأ", string.Join(" | ", errors));

                return View("Index", allChairs);
            }

            if (chairs.ImageFile != null && chairs.ImageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(chairs.ImageFile.FileName)}";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await chairs.ImageFile.CopyToAsync(stream);

                chairs.ImageUrl = $"/uploads/chairs/{fileName}";
            }

            _context.AddOne(chairs);
            SessionMsg(Helper.Success, "تم", "تم إضافة الكرسي بنجاح");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chairs chairs, IFormFile? ImageFile)
        {
            if (id != chairs.Id) return NotFound();

            var existing = _context.FindById(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                int totalItems;
                var allChairs = _context.GetPaged(1, 1000, out totalItems).ToList();

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.EditErrors = errors;
                SessionMsg(Helper.Error, "خطأ", string.Join(" | ", errors));

                chairs.ImageUrl = existing.ImageUrl;
                return View("Index", allChairs);
            }

            existing.Name = chairs.Name;
            existing.Description = chairs.Description;
            existing.Price = chairs.Price;
            existing.IsAvailable = chairs.IsAvailable;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var oldFile = Path.Combine(_env.WebRootPath, "uploads", "chairs", Path.GetFileName(existing.ImageUrl ?? ""));
                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }

                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                Directory.CreateDirectory(uploadsDir);
                var newFilePath = Path.Combine(uploadsDir, newFileName);

                using var stream = new FileStream(newFilePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);

                existing.ImageUrl = $"/uploads/chairs/{newFileName}";
            }

            try
            {
                _context.UpdateOne(existing);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.FindById(id) == null) return NotFound();
                throw;
            }

            SessionMsg(Helper.Success, "تم", "تم تحديث الكرسي بنجاح");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var chairs = await _context.FindByIdasync(id.Value);
            if (chairs == null) return NotFound();

            return View(chairs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null) return NotFound();

            var chair = await _context.FindByIdasync(id.Value);
            if (chair != null)
            {
                var fileName = Path.GetFileName(chair.ImageUrl);
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                var filePath = Path.Combine(uploadsDir, fileName ?? "");

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.DeleteOne(chair);
            }

            SessionMsg(Helper.Success, "تم", "تم حذف الكرسي بنجاح");
            return RedirectToAction(nameof(Index));
        }

        private bool ChairsExists(int id)
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
