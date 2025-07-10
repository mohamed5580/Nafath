using Domin.Entity;
using Domin.Resource;
using Infrastructure.IRepository.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nafath.Controllers
{

    [Area("Admin")]
    public class ChairsManagerController : Controller
    {
        private readonly IRepository<Chairs> _context;

        private readonly IWebHostEnvironment _env;

        public ChairsManagerController(IRepository<Chairs> context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Chairs
        // GET: Admin/ChairsManager
        public IActionResult Index(int page = 1, int pageSize = 1000)
        {
            int totalItems;
            var pagedChairs = _context.GetPaged(page, pageSize, out totalItems)
                              .Where(c => c != null)  // Filter out null items
                              .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(pagedChairs);
        }

        // GET: Chairs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.FindByIdasync(id.Value);
            if (chairs == null)
            {
                return NotFound();
            }

            return View(chairs);
        }

        // GET: Admin/ChairsManager/Create
        // GET: Admin/ChairsManager/Create
        public IActionResult Create()
        {
            return View();
        }

        // CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chairs chairs)
        {
            if (!ModelState.IsValid)
            {
                // Re‑load the full list (page 1, 1000 items) so Index.cshtml can render the table
                int totalItems;
                var allChairs = _context
                    .GetPaged(1, 1000, out totalItems)
                    .ToList();

                // Push your validation errors into ViewBag so Index.cshtml’s alert shows them
                ViewBag.EditErrors = ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
                var str = ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
                SessionMsg(Helper.Success, "خطا", str.ToString());

                // Return the Index view with the full list
                return View("Index", allChairs);
            }

            // File upload handling
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

        // POST: Admin/ChairsManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chairs chairs, IFormFile? ImageFile)
        {
            if (id != chairs.Id)
                return NotFound();

            var existing = _context.FindById(id);
            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                int totalItems;
                var allChairs = _context.GetPaged(1, 1000, out totalItems).ToList();

                ViewBag.EditErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                SessionMsg(Helper.Error, "خطأ", string.Join(" | ", ViewBag.EditErrors));

                chairs.ImageUrl = existing.ImageUrl;

                return View("Index", allChairs);
            }

            // Update fields
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
                if (_context.FindById(id) == null)
                    return NotFound();
                throw;
            }

            SessionMsg(Helper.Success, "تم", "تم تحديث الكرسي بنجاح");
            return RedirectToAction(nameof(Index));
        }





        // GET: Chairs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.FindByIdasync(id.Value);
            if (chairs == null)
            {
                return NotFound();
            }

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
                // build correct physical path to the image
                var fileName = Path.GetFileName(chair.ImageUrl);
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                var filePath = Path.Combine(uploadsDir, fileName);

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
            // Use FindById or FindAll to check existence
            return _context.FindById(id) != null;
        }
        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
