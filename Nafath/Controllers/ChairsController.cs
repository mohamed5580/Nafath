using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nafath.Data;
using Nafath.Models;

namespace Nafath.Controllers
{
    public class ChairsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ChairsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Chairs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Chairs.ToListAsync());
        }

        // GET: Chairs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.Chairs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chairs == null)
            {
                return NotFound();
            }

            return View(chairs);
        }

        // GET: Chairs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Chairs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chairs chairs)
        {
            // First, always check if the submitted data is valid based on your model's annotations
            if (!ModelState.IsValid)
            {
                // If not valid, return the same view. The user will see the validation errors.
                return View(chairs);
            }

            // Handle the file upload ONLY if a file was provided.
            if (chairs.ImageFile != null && chairs.ImageFile.Length > 0)
            {
                // pick a safe filename (GUID avoids name conflicts)
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(chairs.ImageFile.FileName)}";

                // Define the directory to save the file
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chairs");
                // Ensure the directory exists
                Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);

                // Save the file to the server's filesystem
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await chairs.ImageFile.CopyToAsync(stream);
                }

                // store the relative URL that can be used in an <img> tag
                chairs.ImageUrl = $"/uploads/chairs/{fileName}";
            }

            // Add the new chair object to the EF Core context
            _context.Add(chairs);

            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Redirect to the index page to show the updated list of chairs
            return RedirectToAction(nameof(Index));
        }


        // GET: Chairs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.Chairs.FindAsync(id);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ImageUrl,Price,Category,Color,Size,Material,Brand,IsAvailable")] Chairs chairs)
        {
            if (id != chairs.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chairs);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChairsExists(chairs.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(chairs);
        }

        // GET: Chairs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chairs = await _context.Chairs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chairs == null)
            {
                return NotFound();
            }

            return View(chairs);
        }

        // POST: Chairs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chairs = await _context.Chairs.FindAsync(id);
            if (chairs != null)
            {
                _context.Chairs.Remove(chairs);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChairsExists(int id)
        {
            return _context.Chairs.Any(e => e.Id == id);
        }
    }
}
