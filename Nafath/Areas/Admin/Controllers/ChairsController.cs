using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Domin.Entity;
using Infrastructure.IRepository.Base;
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
        public async Task<IActionResult> Index()
        {
            return View(_context.FindAll());
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
            _context.AddOne(chairs);

            // Save all changes to the database
            TempData["Success"] = "the Chair Add succses";
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
                    _context.UpdateOne(chairs);
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
    }
}
