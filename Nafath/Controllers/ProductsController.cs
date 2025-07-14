using Domin.Entity;
using Domin.Resource;
using Infrastructure.IRepository.Base;
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
        public ProductsController(IRepository<Product> context, IRepository<ProductType> typeRepo, IWebHostEnvironment env)
        {
            _productRepo = context;
            _typeRepo = typeRepo;
            _env = env;
        }
        #endregion
        #region Method
        // ProductsController.cs
        public async Task<IActionResult> Index(int? typeId, int page = 1, int pageSize = 12)
        {
            // 1) جلب كل المنتجات مع الـ ProductType في الذاكرة
            var all = await _productRepo.FindAllasync("ProductType"); // IEnumerable<Product>

            // 2) ترشيح (إن وُجد typeId)
            var filtered = typeId.HasValue
                ? all.Where(p => p.ProductTypeId == typeId.Value)
                : all;

            // 3) حساب العدد و Paging
            var totalItems = filtered.Count();
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TypeFilterId = typeId;
            ViewBag.CurrentTypeId = typeId;

            // 4) جلب أنواع المنتجات للقائمة الجانبية
            ViewBag.ProductTypes = await _typeRepo.FindAllasync();

            return View(items);
        }




        // GET: products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var products = await _productRepo.FindByIdasync(id.Value);
            if (products == null)
            {
                return NotFound();
            }

            return View(products);
        }

        // GET: products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product products)
        {
            // First, always check if the submitted data is valid based on your model's annotations
            if (!ModelState.IsValid)
            {
                // If not valid, return the same view. The user will see the validation errors.
                return View(products);
            }

            // Handle the file upload ONLY if a file was provided.
            if (products.ImageFile != null && products.ImageFile.Length > 0)
            {
                // pick a safe filename (GUID avoids name conflicts)
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(products.ImageFile.FileName)}";

                // Define the directory to save the file
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                // Ensure the directory exists
                Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);

                // Save the file to the server's filesystem
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await products.ImageFile.CopyToAsync(stream);
                }

                // store t  he relative URL that can be used in an <img> tag
                products.ImageUrl = $"/uploads/products/{fileName}";
            }

            // Add the new chair object to the EF Core context
            _productRepo.AddOne(products);

            // Save all changes to the database
            TempData["Success"] = "the Chair Add succses";
            // Redirect to the index page to show the updated list of products
            return RedirectToAction(nameof(Index));
        }


        // GET: products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var products = _productRepo.FindById(id);
            if (products == null)
            {
                return NotFound();
            }
            return View(products);
        }

        // POST: products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product products)
        {
            if (id != products.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _productRepo.UpdateOne(products);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!productsExists(products.Id))
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
            return View(products);
        }

            // GET: products/Delete/5
            public async Task<IActionResult> Delete(int? id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                var products = await _productRepo.FindByIdasync(id.Value);
                if (products == null)
                {
                    return NotFound();
                }

                return View(products);
            }

            // POST: products/Delete/5
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteConfirmed(int? id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                var products = await _productRepo.FindByIdasync(id.Value); // Correct method to find the entity by ID
                if (products != null)
                {
                    _productRepo.DeleteOne(products); // Delete the entity
                }

                return RedirectToAction(nameof(Index));
            }

        private bool productsExists(int id)
        {
            // Use FindById or FindAll to check existence
            return _productRepo.FindById(id) != null;
        }
        [HttpPost]
        public IActionResult AddToCart(int id, string name, decimal price, string imageUrl)
        {
            SessionMsg(Helper.Success, "تم بنجاح", ResourceWeb.lbSave);

            return RedirectToAction("Index");
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
