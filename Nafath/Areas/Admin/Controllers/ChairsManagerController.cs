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
            if (id != chairs.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload if a new file is provided
                    if (chairs.ImageFile != null && chairs.ImageFile.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(chairs.ImageFile.FileName)}";
                        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                        Directory.CreateDirectory(uploadsDir);
                        var filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await chairs.ImageFile.CopyToAsync(stream);
                        }

                        chairs.ImageUrl = $"/uploads/chairs/{fileName}";
                    }

                    _context.UpdateOne(chairs);

                    TempData["Success"] = "Chair updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChairsExists(chairs.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            // If we got here, something is wrong - collect errors
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            TempData["Error"] = errors;
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
