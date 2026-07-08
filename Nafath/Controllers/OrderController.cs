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
using System.Security.Claims;
using System.Text.Json;
[Authorize]
public class OrderController : Controller
{
    #region Declaration
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<OrderItem> _orderItemRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    #endregion
    #region Constructor
    public OrderController(
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
                OrderStatus = i.Order.OrderStatus // Assuming OrderStatus is a property of Order

            }).ToList()
        });

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
    public async Task<IActionResult> Checkout(CheckoutViewModel model, string ItemsJson)
    {
        if (!ModelState.IsValid) return View(model);

        var items = string.IsNullOrEmpty(ItemsJson)
                    ? new List<CartItemDto>()
                    : JsonSerializer.Deserialize<List<CartItemDto>>(ItemsJson);

        if (items == null || !items.Any())
        {
            ModelState.AddModelError("", "السلة فارغة.");
            return View(model);
        }

        var userId = _userManager.GetUserId(User);

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            OrderStatus = model.OrderStatus ?? OrderStatuses.PendingReview,
            PaymentMethod = model.PaymentMethod,
            Address = model.Address,
            MobileNumber = model.MobileNumber,
            OrderItems = new List<OrderItem>()
        };

        foreach (var ci in items)
        {
            
            var product = await _productRepo.FindByIdasync(ci.ProductId);
            if (product != null)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = product.Price ?? 0 // نأخذ السعر من الـ DB للأمان
                });
            }
        }

        // AddOneAsync في المستودع تقوم بعمل SaveChangesAsync
        await _orderRepo.AddOneAsync(order);
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

            var orderItem = _orderItemRepo.FindById(id);
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
            try
            {
                _orderItemRepo.UpdateOne(order);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!orderExists(order.Id))
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
        return View(order);
    }
    private bool orderExists(int id)
    {
        // Use FindById or FindAll to check existence
        return _orderRepo.FindById(id) != null;
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
        //1) جلب الطلب نفسه
        var order = await _orderRepo.FindByIdasync(id);
        if (order == null)
            return NotFound();

        //2) جلب بنود الطلب المتعلقة به
        var orderItems = await _orderItemRepo
            .FindByConditionAsync(oi => oi.OrderId == id);

        //3) جلب بيانات المنتجات المرفقة
        var productIds = orderItems.Select(oi => oi.ProductId).Distinct();
        var products = await _productRepo.FindByConditionAsync(p => productIds.Contains(p.Id));

        //4) بناء الـ ViewModel
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
                    ImageUrl = prod.ImageUrl, // أو الخانة المناسبة
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
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

        var orderItems = await _orderItemRepo.FindByIdasync(id.Value);
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

        var orderItems = await _orderItemRepo.FindByIdasync(id.Value); // Correct method to find the entity by ID
        if (orderItems != null)
        {
            _orderItemRepo.DeleteOne(orderItems); // Delete the entity
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

        var order = await _orderRepo.FindByIdasync(id.Value);
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

        var orders = await _orderRepo.FindByIdasync(id.Value); // Correct method to find the entity by ID
        if (orders != null)
        {
            _orderRepo.DeleteOne(orders); // Delete the entity
            SessionMsg(Helper.Success, "تم بنجاح", ResourceWeb.lbSave);
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [Route("Order/UpdateOrder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder(int orderId, string itemsJson)
    {
        // ✅ 1. تحقق من البيانات الواردة أولاً قبل أي عملية
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            SessionMsg(Helper.Error, "خطأ", "لم يتم إرسال بيانات العناصر");
            return RedirectToAction(nameof(Index));
        }

        List<CartItemDto> items;

        try
        {
            items = System.Text.Json.JsonSerializer.Deserialize<List<CartItemDto>>(
                itemsJson,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true   // ✅ يتعامل مع camelCase من JS
                }
            );
        }
        catch (Exception ex)
        {
            SessionMsg(Helper.Error, "خطأ", "فشل في قراءة بيانات العناصر");
            return RedirectToAction(nameof(Index));
        }

        // ✅ 2. التحقق من أن الـ order موجود فعلاً
        var orderExists = await _dbContext.Orders.AnyAsync(o => o.Id == orderId);
        if (!orderExists)
        {
            SessionMsg(Helper.Error, "خطأ", "الطلب غير موجود");
            return RedirectToAction(nameof(Index));
        }

        // ✅ 3. لو السلة فارغة — امسح كل عناصر الطلب
        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // احذف العناصر القديمة
            var existing = await _dbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            _dbContext.OrderItems.RemoveRange(existing);

            // ✅ 4. لو في عناصر جديدة — أضفها
            if (items != null && items.Any())
            {
                var newItems = items.Select(ci => new OrderItem
                {
                    OrderId = orderId,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice
                }).ToList();

                await _dbContext.OrderItems.AddRangeAsync(newItems);
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            SessionMsg(Helper.Success, "تم التحديث بنجاح", ResourceWeb.lbSave);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            SessionMsg(Helper.Error, "خطأ", "حدث خطأ أثناء التحديث: " + ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
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

    public IActionResult GetOrderItems(int orderId)
    {
        var items = _dbContext.OrderItems
            .Where(x => x.OrderId == orderId)
            .Select(x => new
            {
                productId = x.ProductId,
                name = x.Product.Name,
                price = x.UnitPrice,
                count = x.Quantity,
                imageUrl = x.Product.ImageUrl
            })
            .ToList();

        return Json(items);
    }
    #endregion

}
