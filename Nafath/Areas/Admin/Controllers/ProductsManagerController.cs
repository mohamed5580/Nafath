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
        private readonly IRepository<ProductImage> _imageRepo;
        private readonly IWebHostEnvironment _env;

        public ProductsManagerController(
            IRepository<Product> repo,
            IRepository<ProductType> typeRepo,
            IRepository<ProductImage> imageRepo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _typeRepo = typeRepo;
            _imageRepo = imageRepo;
            _env = env;
        }

        public async Task<IActionResult> ProductsByType(int typeId)
        {
            var products = await _repo.Query("ProductType", "Images")
                .Where(p => p.ProductTypeId == typeId)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            var query = _repo.Query("ProductType", "Images").OrderByDescending(p => p.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var productTypes = await _typeRepo.FindAllasync();
            ViewData["ProductTypeId"] = new SelectList(productTypes, "Id", "Name");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.Query("ProductType", "Images")
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateTypesDropDown();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateTypesDropDown(vm.ProductTypeId);

                ViewBag.EditErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var query = _repo.Query("ProductType", "Images").OrderByDescending(p => p.Id);
                var total = await query.CountAsync();
                var items = await query.Take(20).ToListAsync();
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);

                return View("Index", items);
            }

            var product = new Product
            {
                Name = vm.Name,
                ProductTypeId = vm.ProductTypeId,
                Description = vm.Description,
                Price = vm.Price,
                OldPrice = vm.OldPrice,
                SKU = vm.SKU,
                StockQuantity = vm.StockQuantity,
                LowStockThreshold = vm.LowStockThreshold,
                IsAvailable = vm.IsAvailable
            };

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            // Save primary image
            if (vm.ImageFile is { Length: > 0 })
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = System.IO.File.Create(filePath);
                await vm.ImageFile.CopyToAsync(stream);
                product.ImageUrl = $"/uploads/products/{fileName}";
            }

            // Save additional gallery images
            if (vm.AdditionalImages != null && vm.AdditionalImages.Any())
            {
                int order = 1;
                foreach (var imgFile in vm.AdditionalImages.Where(f => f.Length > 0))
                {
                    var galleryFileName = $"{Guid.NewGuid()}{Path.GetExtension(imgFile.FileName)}";
                    var galleryFilePath = Path.Combine(uploadsDir, galleryFileName);
                    await using var stream = System.IO.File.Create(galleryFilePath);
                    await imgFile.CopyToAsync(stream);

                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = $"/uploads/products/{galleryFileName}",
                        DisplayOrder = order++,
                        IsPrimary = false
                    });
                }
            }

            await _repo.AddOneAsync(product);
            SessionMsg(Helper.Success, "تم", "تم إضافة المنتج بنجاح");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.Query("ProductType", "Images").FirstOrDefaultAsync(p => p.Id == id.Value);
            if (product == null) return NotFound();

            await PopulateTypesDropDown(product.ProductTypeId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? ImageFile,
            List<IFormFile>? AdditionalImages)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateTypesDropDown(product.ProductTypeId);
                return View(product);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            // Update primary image if new one provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = $"/uploads/products/{fileName}";
            }

            // Save additional gallery images
            if (AdditionalImages != null && AdditionalImages.Any())
            {
                int order = 1;
                foreach (var imgFile in AdditionalImages.Where(f => f.Length > 0))
                {
                    var galleryFileName = $"{Guid.NewGuid()}{Path.GetExtension(imgFile.FileName)}";
                    var galleryFilePath = Path.Combine(uploadsDir, galleryFileName);
                    await using var stream = System.IO.File.Create(galleryFilePath);
                    await imgFile.CopyToAsync(stream);

                    await _imageRepo.AddOneAsync(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = $"/uploads/products/{galleryFileName}",
                        DisplayOrder = order++,
                        IsPrimary = false
                    });
                }
            }

            try
            {
                await _repo.UpdateOneAsync(product);
                SessionMsg(Helper.Success, "تم", "تم تحديث المنتج بنجاح");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _repo.FindByIdasync(id) == null)
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGalleryImage(int id)
        {
            var image = await _imageRepo.FindByIdasync(id);
            if (image != null)
            {
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                await _imageRepo.DeleteOneAsync(image);
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Image not found." });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.FindByIdasync(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _repo.Query("Images").FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                // Delete primary image
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                // Delete gallery images
                foreach (var img in product.Images)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                }

                await _repo.DeleteOneAsync(product);
            }

            return RedirectToAction(nameof(Index));
        }

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
