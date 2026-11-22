using Domin.Entity;             // Order, OrderItem
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
[Authorize]
public class OrderController : Controller
{
    #region Declaration
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    #endregion
    #region Constructor
    public OrderController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }
    #endregion
    #region Method

    public async Task<IActionResult> Index()
    {

        var userId = _userManager.GetUserId(User);

        // 2) query only *their* orders, including items & products
        var myOrders = await _dbContext.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Colors)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();


        // 3) project to your VM
        var vmList = myOrders.Select(o => new OrderDetailsViewModel
        {
            Order = o,
            Items = o.OrderItems.Select(i => new OrderItemDetailDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "[لا يوجد]",
                ImageUrl = i.Product?.ImageUrl ?? "/images/default.png",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Size = i.Size,
                Color = i.Color,
                ColorCode = ResolveColorCode(i.Color, i.ColorCode, i.Product),
                OrderStatus = i.Order.OrderStatus // Assuming OrderStatus is a property of Order

            }).ToList()
        });

        ViewBag.ClearShoppingCart = TempData["ClearShoppingCart"] is true;
        return View(vmList);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new CheckoutViewModel
        {
            Name = user.FullName +" "+ user.LastName ?? user.UserName,
            Email = user.Email,
            MobileNumber = user.PhoneNumber,
            Address = user.Address,
            Items = new List<CartItemDto>()
        };
        return View(model);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, string? ItemsJson)
    {
        if (!ModelState.IsValid) return View(model);

        List<CartItemDto> submittedItems;
        try
        {
            submittedItems = string.IsNullOrWhiteSpace(ItemsJson)
                ? new List<CartItemDto>()
                : JsonSerializer.Deserialize<List<CartItemDto>>(
                    ItemsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CartItemDto>();
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "تعذر قراءة محتويات السلة. يرجى تحديث الصفحة والمحاولة مرة أخرى.");
            return View(model);
        }

        if (submittedItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "السلة فارغة.");
            return View(model);
        }

        if (submittedItems.Any(item => item == null || item.ProductId <= 0 || item.Quantity <= 0))
        {
            ModelState.AddModelError(string.Empty, "تحتوي السلة على منتج أو كمية غير صالحة.");
            return View(model);
        }

        var quantities = submittedItems
            .GroupBy(item => new
            {
                item.ProductId,
                Size = NormalizeSize(item.Size),
                Color = NormalizeColor(item.Color),
                ColorCode = NormalizeColorCode(item.ColorCode)
            })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.Size,
                group.Key.Color,
                group.Key.ColorCode,
                Quantity = group.Sum(item => (long)item.Quantity)
            })
            .ToList();

        if (quantities.Any(item => item.Quantity > int.MaxValue))
        {
            ModelState.AddModelError(string.Empty, "تتجاوز كمية أحد المنتجات الحد المسموح.");
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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

            var availableSizes = product.Sizes;
            if (availableSizes.Count > 0 && (item.Size == null || !availableSizes.Contains(item.Size, StringComparer.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, $"يرجى اختيار مقاس صالح للمنتج «{product.Name}».");
                return View(model);
            }

            if (availableSizes.Count == 0 && item.Size != null)
            {
                ModelState.AddModelError(string.Empty, $"المنتج «{product.Name}» لا يدعم اختيار مقاس.");
                return View(model);
            }

            if (!product.Price.HasValue || product.Price <= 0)
            {
                ModelState.AddModelError(string.Empty, $"تعذر إتمام الطلب لأن سعر المنتج «{product.Name}» غير صالح.");
                return View(model);
            }
        }

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
                Size = item.Size,
                Color = item.Color,
                ColorCode = item.ColorCode,
                Quantity = (int)item.Quantity,
                UnitPrice = products[item.ProductId].Price!.Value
            }).ToList()
        };

        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["ClearShoppingCart"] = true;
        SessionMsg(Helper.Success, "تم إنشاء الطلب بنجاح", ResourceWeb.lbSave);

        return RedirectToAction("Index");
    }
    // GET: order/Edit/5
    public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var orderItem = await _dbContext.OrderItems
                .Include(item => item.Order)
                .FirstOrDefaultAsync(item => item.Id == id && item.Order.UserId == userId);
            if (orderItem == null)
            {
                return NotFound();
            }
            return View(orderItem);
        }

    // POST: order/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderItem order)
    {
        if (id != order.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            var existingItem = await _dbContext.OrderItems
                .Include(item => item.Order)
                .FirstOrDefaultAsync(item => item.Id == id && item.Order.UserId == userId);
            if (existingItem == null) return NotFound();

            existingItem.Quantity = order.Quantity;
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(order);
    }
    public IActionResult Confirmation(int id)
    {
        TempData["SuccessOrderId"] = id;
        return RedirectToAction("Index");
    }
    // OrderController.cs
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(p => p.Colors)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        if (order == null)
            return NotFound();

        var vm = new OrderDetailsViewModel
        {
            Order = order,
            Items = order.OrderItems.Select(item =>
            {
                return new OrderItemDetailDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "[لا يوجد]",
                    ImageUrl = item.Product?.ImageUrl ?? "/images/default.png",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Size = item.Size,
                    Color = item.Color,
                    ColorCode = ResolveColorCode(item.Color, item.ColorCode, item.Product),
                    OrderStatus = order.OrderStatus
                };
            }).ToList()
        };

        return View(vm);
    }

    // GET: products/Delete/5
    public async Task<IActionResult> DeleteOrderItem(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var orderItems = await _dbContext.OrderItems
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.Id == id.Value && item.Order.UserId == userId);
        if (orderItems == null)
        {
            return NotFound();
        }

        return View(orderItems);
    }

    // POST: products/Delete/5
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrderItemConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var orderItems = await _dbContext.OrderItems
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.Id == id.Value && item.Order.UserId == userId);
        if (orderItems != null)
        {
            _dbContext.OrderItems.Remove(orderItems);
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
    // GET: products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == id.Value && order.UserId == userId);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }



    // POST: products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var orders = await _dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == id.Value && order.UserId == userId);
        if (orders != null)
        {
            _dbContext.Orders.Remove(orders);
            await _dbContext.SaveChangesAsync();
            SessionMsg(Helper.Success, "تم بنجاح", ResourceWeb.lbSave);
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [Route("Order/UpdateOrder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder(int orderId, string itemsJson)
    {
        // 1. التحقق من البيانات
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            SessionMsg(Helper.Error, "خطأ", "لم يتم إرسال بيانات العناصر");
            return RedirectToAction(nameof(Index));
        }

        List<CartItemDto>? items;

        try
        {
            items = System.Text.Json.JsonSerializer.Deserialize<List<CartItemDto>>(
                itemsJson,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }
        catch
        {
            SessionMsg(Helper.Error, "خطأ", "فشل في قراءة بيانات العناصر");
            return RedirectToAction(nameof(Index));
        }

        if (items == null)
        {
            SessionMsg(Helper.Error, "خطأ", "بيانات الطلب غير صالحة");
            return RedirectToAction(nameof(Index));
        }

        // 2. التحقق من ملكية الطلب
        var userId = _userManager.GetUserId(User);

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            SessionMsg(Helper.Error, "خطأ", "الطلب غير موجود");
            return RedirectToAction(nameof(Index));
        }

        // 3. التحقق من المنتجات والكميات
        if (items.Any(item =>
            item == null ||
            item.ProductId <= 0 ||
            item.Quantity <= 0))
        {
            SessionMsg(Helper.Error, "خطأ", "تحتوي بيانات الطلب على منتج أو كمية غير صالحة");
            return RedirectToAction(nameof(Index));
        }

        // التحقق من عدم تكرار نفس المنتج بنفس الخيارات
        var duplicateItems = items
            .GroupBy(item => new
            {
                item.ProductId,
                Size = NormalizeSize(item.Size),
                Color = NormalizeColor(item.Color),
                ColorCode = item.ColorCode?.Trim()
            })
            .Any(group => group.Count() > 1);

        if (duplicateItems)
        {
            SessionMsg(Helper.Error, "خطأ", "يوجد منتج مكرر بنفس المقاس واللون");
            return RedirectToAction(nameof(Index));
        }

        var productIds = items
            .Select(item => item.ProductId)
            .Distinct()
            .ToArray();

        var products = await _dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        // 4. التحقق من توفر المنتجات
        if (items.Any(item =>
            !products.TryGetValue(item.ProductId, out var product) ||
            !product.IsAvailable ||
            !product.Price.HasValue ||
            product.Price <= 0))
        {
            SessionMsg(Helper.Error, "خطأ", "أحد المنتجات أو الأسعار لم يعد متاحاً");
            return RedirectToAction(nameof(Index));
        }

        // التحقق من إجمالي الكمية المطلوبة لكل منتج
        var quantitiesByProduct = items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                TotalQuantity = group.Sum(item => item.Quantity)
            });

        if (quantitiesByProduct.Any(group =>
            products[group.ProductId].StockQuantity < group.TotalQuantity))
        {
            SessionMsg(Helper.Error, "خطأ", "الكمية المطلوبة تتجاوز المخزون المتاح");
            return RedirectToAction(nameof(Index));
        }

        // 5. التحقق من المقاسات
        if (items.Any(item =>
        {
            var product = products[item.ProductId];
            var size = NormalizeSize(item.Size);
            var available = product.Sizes;

            return (available.Count > 0 &&
                    (size == null ||
                     !available.Contains(size, StringComparer.OrdinalIgnoreCase)))
                   ||
                   (available.Count == 0 && size != null);
        }))
        {
            SessionMsg(Helper.Error, "خطأ", "أحد المنتجات يحتاج إلى مقاس صالح");
            return RedirectToAction(nameof(Index));
        }

        // 6. تحديث الطلب
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var existingItems = await _dbContext.OrderItems
                .Where(item => item.OrderId == orderId)
                .ToListAsync();

            _dbContext.OrderItems.RemoveRange(existingItems);

            // لو items فارغة، سيتم حذف كل عناصر الطلب
            if (items.Any())
            {
                var newItems = items.Select(item => new OrderItem
                {
                    OrderId = orderId,
                    ProductId = item.ProductId,

                    Size = NormalizeSize(item.Size),
                    Color = NormalizeColor(item.Color),

                    // الإضافة المهمة لحفظ كود اللون
                    ColorCode = string.IsNullOrWhiteSpace(item.ColorCode)
                        ? null
                        : item.ColorCode.Trim(),

                    Quantity = item.Quantity,

                    // لا نعتمد على السعر القادم من JavaScript
                    UnitPrice = products[item.ProductId].Price!.Value

                }).ToList();

                await _dbContext.OrderItems.AddRangeAsync(newItems);
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            SessionMsg(
                Helper.Success,
                "تم التحديث بنجاح",
                ResourceWeb.lbSave
            );
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            // الأفضل تسجيل تفاصيل الخطأ في Logger
            SessionMsg(
                Helper.Error,
                "خطأ",
                "حدث خطأ أثناء تحديث الطلب"
            );
        }

        return RedirectToAction(nameof(Index));
    }
    private static string? NormalizeSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return null;
        var trimmed = size.Trim();
        var match = ProductSizeOptions.All.FirstOrDefault(s => s.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        return match ?? trimmed;
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        return color.Trim();
    }

    private static string? NormalizeColorCode(string? colorCode)
    {
        if (string.IsNullOrWhiteSpace(colorCode)) return null;
        return colorCode.Trim();
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);

        if (order == null)
        {
            return Json(new
            {
                success = false,
                message = "الطلب غير موجود"
            });
        }

        order.OrderStatus = orderStatus;

        await _dbContext.SaveChangesAsync();
        SessionMsg(Helper.Success, "تم بنجاح", ResourceWeb.lbSave);
        return Json(new
        {
            success = true,
            newStatus = orderStatus,
            message = "تم التعديل بنجاح"
        });
    }



    private void SessionMsg(string MsgType, string Title, string Msg)
    {
        HttpContext.Session.SetString(Helper.MsgType, MsgType);
        HttpContext.Session.SetString(Helper.Title, Title);
        HttpContext.Session.SetString(Helper.Msg, Msg);
    }

    public async Task<IActionResult> GetOrderItems(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        if (!await _dbContext.Orders.AnyAsync(order => order.Id == orderId && order.UserId == userId))
        {
            return NotFound();
        }

        var orderItems = await _dbContext.OrderItems
            .Where(x => x.OrderId == orderId)
            .Include(x => x.Product)
                .ThenInclude(p => p.Colors)
            .ToListAsync();

        var items = orderItems.Select(x => new
        {
            productId = x.ProductId,
            name = x.Product?.Name,
            price = x.UnitPrice,
            count = x.Quantity,
            imageUrl = x.Product?.ImageUrl,
            size = x.Size,
            color = x.Color,
            colorCode = ResolveColorCode(x.Color, x.ColorCode, x.Product)
        }).ToList();

        return Json(items);
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
    #endregion

}
