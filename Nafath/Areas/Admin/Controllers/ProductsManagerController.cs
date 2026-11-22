using Domin.Entity;
using Domin.Resource;
using Infrastructure.IRepository.Base;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsManagerController : Controller
    {
        private readonly IRepository<Product> _repo;
        private readonly IRepository<ProductType> _typeRepo;
        private readonly IRepository<ProductImage> _imageRepo;
        private readonly IRepository<ProductColor> _colorRepo;
        private readonly IWebHostEnvironment _env;

        public ProductsManagerController(
            IRepository<Product> repo,
            IRepository<ProductType> typeRepo,
            IRepository<ProductImage> imageRepo,
            IRepository<ProductColor> colorRepo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _typeRepo = typeRepo;
            _imageRepo = imageRepo;
            _colorRepo = colorRepo;
            _env = env;
        }

        public async Task<IActionResult> ProductsByType(int typeId)
        {
            var products = await _repo.Query("ProductType", "Images", "Colors")
                .Where(p => p.ProductTypeId == typeId)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            var query = _repo.Query("ProductType", "Images", "Colors").OrderByDescending(p => p.Id);
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
            var product = await _repo.Query("ProductType", "Images", "Colors")
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

            var allSizes = (vm.Sizes ?? new List<string>()).ToList();
            if (!string.IsNullOrWhiteSpace(vm.CustomSizes))
            {
                allSizes.AddRange(vm.CustomSizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
                AvailableSizes = ProductSizeOptions.Serialize(allSizes),
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

            // Save colors
            if (!string.IsNullOrWhiteSpace(vm.ColorsJson))
            {
                var colorDtos = ParseColorsJson(vm.ColorsJson);
                int colorOrder = 0;
                foreach (var dto in colorDtos)
                {
                    await _colorRepo.AddOneAsync(new ProductColor
                    {
                        ProductId = product.Id,
                        ColorName = dto.ColorName.Trim(),
                        HexCode = dto.HexCode.Trim(),
                        DisplayOrder = colorOrder++
                    });
                }
            }

            SessionMsg(Helper.Success, "تم", "تم إضافة المنتج بنجاح");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _repo.Query("ProductType", "Images", "Colors").FirstOrDefaultAsync(p => p.Id == id.Value);
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
            List<IFormFile>? AdditionalImages,
            List<string>? sizes,
            string? customSizes,
            string? colorsJson)
        {
            if (id != product.Id) return NotFound();

            // Clear navigation collections and auxiliary properties from ModelState validation
            ModelState.Remove("ProductType");
            ModelState.Remove("Images");
            ModelState.Remove("Colors");
            ModelState.Remove("OrderItems");
            ModelState.Remove("ImageFile");

            var allSizes = (sizes ?? new List<string>()).ToList();
            if (!string.IsNullOrWhiteSpace(customSizes))
            {
                allSizes.AddRange(customSizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            product.AvailableSizes = ProductSizeOptions.Serialize(allSizes);

            if (!ModelState.IsValid)
            {
                await PopulateTypesDropDown(product.ProductTypeId);
                var existingWithImages = await _repo.Query("Images", "Colors").FirstOrDefaultAsync(p => p.Id == id);
                if (existingWithImages != null)
                {
                    product.Images = existingWithImages.Images;
                    product.Colors = existingWithImages.Colors;
                }
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

            // Sync colors: delete existing, re-insert from JSON
            var existingColors = await _colorRepo.Query()
                .Where(c => c.ProductId == id)
                .ToListAsync();
            if (existingColors.Any())
                await _colorRepo.DeleteRangeAsync(existingColors);

            if (!string.IsNullOrWhiteSpace(colorsJson))
            {
                var colorDtos = ParseColorsJson(colorsJson);
                int colorOrder = 0;
                foreach (var dto in colorDtos)
                {
                    await _colorRepo.AddOneAsync(new ProductColor
                    {
                        ProductId = product.Id,
                        ColorName = dto.ColorName.Trim(),
                        HexCode = dto.HexCode.Trim(),
                        DisplayOrder = colorOrder++
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

        /// <summary>
        /// Parses a JSON array of color objects from the hidden form field.
        /// Expected shape: [{"colorName":"Red","hexCode":"#FF0000"}, ...]
        /// </summary>
        private static List<ProductColorDto> ParseColorsJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<ProductColorDto>>(json, options) ?? new();
            }
            catch
            {
                return new();
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteColor(int id)
        {
            var color = await _colorRepo.FindByIdasync(id);
            if (color != null)
            {
                await _colorRepo.DeleteOneAsync(color);
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "اللون غير موجود." });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductColors(int productId)
        {
            var colors = await _colorRepo.Query()
                .Where(c => c.ProductId == productId)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Id, c.ColorName, c.HexCode, c.DisplayOrder })
                .ToListAsync();
            return Json(colors);
        }
    }
}
