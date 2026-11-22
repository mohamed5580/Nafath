using Domin.Entity;             // Order, OrderItem
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.IRepository.Base;
using Infrastructure.Models;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OrderManageController : Controller
    {
        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<OrderItem> _orderItemRepo;
        private readonly IRepository<Product> _productRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public OrderManageController(
            ApplicationDbContext dbContext,
            IRepository<Order> orderRepo,
            IRepository<OrderItem> orderItemRepo,
            IRepository<Product> productRepo,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _productRepo = productRepo;
            _userManager = userManager;
        }
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 20;
            page = Math.Clamp(page, 1, int.MaxValue);

            // 1) عدّ الطلبات
            int totalOrders = await _orderRepo.CountAsync();
            int totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = Math.Clamp(page, 1, Math.Max(1, totalPages));

            // 2) جلب مع Pagination و الـ Includes
            var orders = await _orderRepo.GetPaginatedAsync(
                page,
                pageSize,
                include: q => q
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Colors)
                    .OrderByDescending(o => o.OrderDate)
            );

            // 3) تحويل إلى ViewModel
            var vmList = orders.Select(o => new OrderManagerDetailsViewModel
            {
                Order = o,

                UserName = o.User?.FullName +" "+ o.User?.LastName ?? "",
                UserEmail = o.User?.Email ?? "",
                UserPhone = o.MobileNumber ?? "",
                UserAdress = o.Address ?? "",
                PaymentMethod = o.PaymentMethod ?? "",

                Items = o.OrderItems.Select(i => new OrderItemDetailDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "[لا يوجد]",
                    ImageUrl = i.Product?.ImageUrl ?? "/images/default.png",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Size = i.Size,
                    Color = i.Color,
                    ColorCode = ResolveColorCode(i.Color, i.ColorCode, i.Product)
                }).ToList()
            }).ToList();

            return View(vmList);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new CheckoutViewModel
            {
                Name = user.FullName ?? user.UserName,
                Email = user.Email,
                MobileNumber = user.PhoneNumber,
                Address = user.Address,
                Items = new List<CartItemDto>()
            };
            SessionMsg(Helper.Success, "تم بنجاح", ResourceWeb.lbSave);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model, string? ItemsJson)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.Items = string.IsNullOrWhiteSpace(ItemsJson)
                    ? new List<CartItemDto>()
                    : JsonConvert.DeserializeObject<List<CartItemDto>>(ItemsJson) ?? new List<CartItemDto>();
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "تعذر قراءة محتويات السلة.");
                return View(model);
            }

            if (model.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "السلة فارغة.");
                return View(model);
            }

            if (model.Items.Any(item => item == null || item.ProductId <= 0 || item.Quantity <= 0))
            {
                ModelState.AddModelError(string.Empty, "تحتوي السلة على منتج أو كمية غير صالحة.");
                return View(model);
            }

            var quantities = model.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => (long)item.Quantity)
                })
                .ToList();

            if (quantities.Any(item => item.Quantity > int.MaxValue))
            {
                ModelState.AddModelError(string.Empty, "تتجاوز كمية أحد المنتجات الحد المسموح.");
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var productIds = quantities.Select(item => item.ProductId).ToArray();
            var products = await _dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);

            foreach (var item in quantities)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    ModelState.AddModelError(string.Empty, "أحد المنتجات الموجودة في السلة لم يعد متاحاً.");
                    return View(model);
                }

                if (!product.IsAvailable || product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"الكمية المطلوبة من المنتج «{product.Name}» غير متوفرة حالياً.");
                    return View(model);
                }

                if (!product.Price.HasValue || product.Price <= 0)
                {
                    ModelState.AddModelError(string.Empty, $"تعذر إتمام الطلب لأن سعر المنتج «{product.Name}» غير صالح.");
                    return View(model);
                }
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                OrderStatus = OrderStatuses.PendingReview,
                PaymentMethod = model.PaymentMethod,
                Address = model.Address,
                MobileNumber = model.MobileNumber,
                OrderItems = quantities.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = (int)item.Quantity,
                    UnitPrice = products[item.ProductId].Price!.Value
                }).ToList()
            };

            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var orderItem = _orderItemRepo.FindById(id);
            if (orderItem == null) return NotFound();

            return View(orderItem);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderItem order)
        {
            if (id != order.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _orderItemRepo.UpdateOne(order);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!orderExists(order.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        private bool orderExists(int id)
        {
            return _orderRepo.FindById(id) != null;
        }

        public IActionResult Confirmation(int id)
        {
            TempData["SuccessOrderId"] = id;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepo.FindByIdasync(id);
            if (order == null) return NotFound();

            var orderItems = await _orderItemRepo.FindByConditionAsync(oi => oi.OrderId == id);
            var productIds = orderItems.Select(oi => oi.ProductId).Distinct();
            var products = await _dbContext.Products
                .Include(p => p.Colors)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var vm = new OrderDetailsViewModel
            {
                Order = order,
                Items = orderItems.Select(oi =>
                {
                    var prod = products.First(p => p.Id == oi.ProductId);
                    return new OrderItemDetailDto
                    {
                        ProductId = prod.Id,
                        ProductName = prod.Name,
                        ImageUrl = prod.ImageUrl,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Size = oi.Size,
                        Color = oi.Color,
                        ColorCode = ResolveColorCode(oi.Color, oi.ColorCode, prod)
                    };
                }).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> DeleteOrderItem(int? id)
        {
            if (id == null) return NotFound();

            var orderItems = await _orderItemRepo.FindByIdasync(id.Value);
            if (orderItems == null) return NotFound();

            return View(orderItems);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrderItemConfirmed(int? id)
        {
            if (id == null) return NotFound();

            var orderItems = await _orderItemRepo.FindByIdasync(id.Value);
            if (orderItems != null)
            {
                _orderItemRepo.DeleteOne(orderItems);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _orderRepo.FindByIdasync(id.Value);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderRepo.FindByIdasync(id);
            if (order == null)
                return NotFound();

            await _orderRepo.DeleteOneAsync(order);
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null) return NotFound();

            var orders = await _orderRepo.FindByIdasync(id.Value);
            if (orders != null)
            {
                _orderRepo.DeleteOne(orders);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus)
        {
            if (!OrderStatuses.IsAllowed(orderStatus))
                return Json(new { success = false, message = "حالة غير صالحة" });

            var order = await _orderRepo.FindByIdasync(orderId);
            if (order == null)
                return Json(new { success = false, message = "الطلب غير موجود" });

            order.OrderStatus = orderStatus;
            await _orderRepo.UpdateOneAsync(order);

            return Json(new { success = true, newStatus = orderStatus });
        }

        private static string? ResolveColorCode(string? color, string? colorCode, Product? product)
        {
            if (!string.IsNullOrWhiteSpace(colorCode))
            {
                return colorCode.Trim();
            }

            if (string.IsNullOrWhiteSpace(color) || product?.Colors == null)
            {
                return null;
            }

            return product.Colors
                .FirstOrDefault(c => string.Equals(c.ColorName, color.Trim(), StringComparison.OrdinalIgnoreCase))
                ?.HexCode;
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
