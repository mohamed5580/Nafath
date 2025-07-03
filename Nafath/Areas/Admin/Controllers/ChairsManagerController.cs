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
        public IActionResult Index(int page = 1, int pageSize = 12)
        {
            int totalItems;
            var pagedChairs = _context.GetPaged(page, pageSize, out totalItems);

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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ChairsManager/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chairs chairs)
        {
            if (!ModelState.IsValid)
            {
                // If validation failed, re-display form with errors
                SessionMsg(Helper.Error, "خطأ", "لا يمكن ادخال قيم فارغة");
                // If not valid, return the same view. The user will see the validation errors.
                return RedirectToAction(nameof(Index));
            }

            // Handle file upload if provided
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
            if (chairs.Id == null || chairs.Name == string.Empty || chairs.Price == null || chairs.Description == string.Empty)
            {
                SessionMsg(Helper.Error, "خطأ", "لا يمكن ادخال قيم فارغة");
                // If not valid, return the same view. The user will see the validation errors.

                return RedirectToAction(nameof(Index));
            }
            else
            {



                _context.AddOne(chairs);

                TempData["Success"] = "Chair added successfully.";
                return RedirectToAction(nameof(Index));
            }

            SessionMsg(Helper.Error, "خطأ", "لا يمكن ادخال قيم فارغة");

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

            // 1) Load existing
            var existing = _context.FindById(id);
            if (existing == null) return NotFound();

            // 2) Check for no‑op:
            var nothingChanged =
                chairs.ImageFile == null &&
                chairs.Name == existing.Name &&
                chairs.Description == existing.Description &&
                chairs.Price == existing.Price &&
                chairs.IsAvailable == existing.IsAvailable;

            if (nothingChanged)
            {
                TempData["Info"] = "لم يتم تعديل أي شيء";
                SessionMsg(Helper.Msg, "تنبيه","لا يوجد تغييرات" );
                return RedirectToAction(nameof(Index));
            }

            // 3) Validate ModelState
            if (!ModelState.IsValid)
            {
                // ensure Edit.cshtml exists under Areas/Admin/Views/ChairsManager/
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                SessionMsg(Helper.Error, "خطأ", string.Join("; ", errors));
                return View(chairs);
            }

            // 4) Merge and handle upload
            existing.Name = chairs.Name;
            existing.Description = chairs.Description;
            existing.Price = chairs.Price;
            existing.IsAvailable = chairs.IsAvailable;

            if (chairs.ImageFile?.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(chairs.ImageFile.FileName)}";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                Directory.CreateDirectory(uploadsDir);

                using var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create);
                await chairs.ImageFile.CopyToAsync(stream);
                existing.ImageUrl = $"/uploads/chairs/{fileName}";
            }

            // 5) Save and redirect
            _context.UpdateOne(existing);
            TempData["Success"] = "Chair updated successfully";
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
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.FindByIdasync(id.Value); // Correct method to find the entity by ID
            if (chairs != null)
            {
                _context.DeleteOne(chairs); // Delete the entity
            }

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
