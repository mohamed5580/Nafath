using Domin.Entity;
using Domin.Resource;
using Infrastructure.IRepository.Base;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsManagerController : Controller
    {
        private readonly IRepository<Product> _repo;
        private readonly IRepository<ProductType> _typeRepo;
        private readonly IWebHostEnvironment _env;

        public ProductsManagerController(
            IRepository<Product> repo,
            IRepository<ProductType> typeRepo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _typeRepo = typeRepo;
            _env = env;
        }
        public async Task<IActionResult> ProductsByType(int typeId)
        {
            var products = await _repo.FindByConditionAsync(p => p.ProductTypeId == typeId);
            return View(products);
        }


        // GET: Admin/ProductsManager
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var (items, total) = await _repo.GetPagedAsync(page, pageSize);

            // استخدام typeRepo بدلاً من _context
            var productTypes = await _typeRepo.FindAllasync();

            ViewData["ProductTypeId"] = new SelectList(productTypes, "Id", "Name");

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(items);
        }


        // GET: Admin/ProductsManager/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.SelectOneAsync(
                              p => p.Id == id,
                              p => p.ProductType);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET
        public async Task<IActionResult> Create()
        {
            await PopulateTypesDropDown();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateVm vm)
        {
            // 1. Если модель не валидна — возвращаем Index с ошибками и нужными данными
            if (!ModelState.IsValid)
            {
                await PopulateTypesDropDown(vm.ProductTypeId);

                ViewBag.EditErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var (items, total) = await _repo.GetPagedAsync(1, 1000);
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = (int)Math.Ceiling(total / 1000.0);

                return View("Index", items);
            }

            // 2. Маппим VM → Entity
            var product = new Product
            {
                Name = vm.Name,
                ProductTypeId = vm.ProductTypeId,
                Description = vm.Description,
                Price = vm.Price,
                IsAvailable = vm.IsAvailable
            };

            // 3. Если пользователь загрузил файл — сохраняем его и ставим URL
            if (vm.ImageFile is { Length: > 0 })
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.ImageFile.FileName)}";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = System.IO.File.Create(filePath);
                await vm.ImageFile.CopyToAsync(stream);

                product.ImageUrl = $"/uploads/products/{fileName}";
            }

            // 4. Сохраняем запись
            await _repo.AddOneAsync(product);
            SessionMsg(Helper.Success, "تم", "تم إضافة المنتج بنجاح");

            // 5. Редирект на Index
            return RedirectToAction(nameof(Index));
        }




        // GET: Admin/ProductsManager/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.FindByIdasync(id.Value);
            if (product == null) return NotFound();

            await PopulateTypesDropDown(product.ProductTypeId);
            return View(product);
        }

        // POST: Admin/ProductsManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? ImageFile)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateTypesDropDown(product.ProductTypeId);
                return View(product);
            }

            // تحديث الصورة إن وُجدت
            if (ImageFile != null)
            {
                // (نفس منطق الحفظ أعلاه)
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
                using var stream = System.IO.File.Create(path);
                await ImageFile.CopyToAsync(stream);
                product.ImageUrl = "/uploads/" + fileName;
            }

            try
            {
                await _repo.UpdateOneAsync(product);
               
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _repo.FindByIdasync(id) == null)
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/ProductsManager/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.FindByIdasync(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Admin/ProductsManager/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _repo.FindByIdasync(id);
            if (product != null)
                await _repo.DeleteOneAsync(product);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// يحمّل قائمة الأنواع في ViewData["ProductTypeId"] للاستعمال في DropDownList
        /// </summary>
        private async Task PopulateTypesDropDown(object? selectedType = null)
        {
            var types = await _typeRepo.FindAllasync();
            ViewData["ProductTypeId"] = new SelectList(types, "Id", "Name", selectedType);
        }
        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }

    }
}
