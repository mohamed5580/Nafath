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
                SessionMsg(Helper.Error, "خطأ", "الرجاء ملء جميع الحقول المطلوبة");
                return View(chairs);
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



        // GET: Chairs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = _context.FindById(id);
            if (chairs == null)
            {
                return NotFound();
            }
            return View(chairs);
        }

        // POST: Chairs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Chairs chairs)
        {
            if (id != chairs.Id) return NotFound();

            var existing = _context.FindById(id);
            if (existing == null) return NotFound();

            // Check for changes
            var changesExist =
                chairs.ImageFile != null ||
                chairs.Name != existing.Name ||
                chairs.Description != existing.Description ||
                chairs.Price != existing.Price ||
                chairs.IsAvailable != existing.IsAvailable;

            if (!changesExist)
            {
                SessionMsg(Helper.Msg, "تنبيه", "لا يوجد تغييرات");
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                SessionMsg(Helper.Error, "خطأ", string.Join("; ", errors));
                return View(chairs);
            }

            // Update properties
            existing.Name = chairs.Name;
            existing.Description = chairs.Description;
            existing.Price = chairs.Price;
            existing.IsAvailable = chairs.IsAvailable;

            // Handle new image
            if (chairs.ImageFile?.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new image
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(chairs.ImageFile.FileName)}";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                var filePath = Path.Combine(uploadsDir, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await chairs.ImageFile.CopyToAsync(stream);
                existing.ImageUrl = $"/uploads/chairs/{fileName}";
            }

            _context.UpdateOne(existing);
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

        // POST: Chairs/Delete/5
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null) return NotFound();

            var chair = await _context.FindByIdasync(id.Value);
            if (chair != null)
            {
                // Delete associated image
                if (!string.IsNullOrEmpty(chair.ImageUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, chair.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
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
