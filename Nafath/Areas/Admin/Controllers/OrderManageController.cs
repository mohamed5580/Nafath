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

[Authorize]
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
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 20;
        page = Math.Clamp(page, 1, int.MaxValue);

        // 1) عدّ الطلبات
        int totalOrders = await _orderRepo.CountAsync();
        ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        ViewBag.CurrentPage = Math.Clamp(page, 1, ViewBag.TotalPages);

        // 2) جلب مع Pagination و الـ Includes
        var orders = await _orderRepo.GetPaginatedAsync(
            page,
            pageSize,
            include: q => q
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
        );

        // 3) تحويل إلى ViewModel
        var vmList = orders.Select(o => new OrderDetailsViewModel
        {
            Order = o,
            Items = o.OrderItems.Select(i => new OrderItemDetailDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "[لا يوجد]",
                ImageUrl = i.Product?.ImageUrl ?? "/images/default.png",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
                // TotalPrice يحسبه الـ DTO
            }).ToList()
        })
        .ToList();

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
    public async Task<IActionResult> Checkout(CheckoutViewModel model, string ItemsJson)
    {
        if (!ModelState.IsValid)
            return View(model);
        model.Items = JsonConvert
            .DeserializeObject<List<CartItemDto>>(ItemsJson)
            ?? new List<CartItemDto>();
        if (!model.Items.Any())
        {
            ModelState.AddModelError("", "السلة فارغة.");
            return View(model);
        }

        // 1) افتح معاملة
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 2) أنشئ الطلب
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = new Order { UserId = userId, OrderDate = DateTime.Now };
            await _orderRepo.AddOneAsync(order);   // لا تقم بSaveChanges هنا إن أمكن

            // 3) أضف بنود الطلب دفعة واحدة
            var items = new List<OrderItem>();
            foreach (var ci in model.Items)
            {
                var product = await _productRepo.FindByIdasync(ci.ProductId);
                if (product == null)
                    throw new Exception($"المنتج {ci.ProductId} غير موجود.");

                items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice    // استخدم القيمة الافتراضية إذا كانت null
                });
            }
            await _orderItemRepo.AddRangeAsync(items);  // أو loop مع AddOneAsync بدون SaveChanges داخلها

            // 4) حفظ التغييرات مرة واحدة
            await _dbContext.SaveChangesAsync();

            // 5) اكتمال المعاملة
            await transaction.CommitAsync();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        catch
        {
            // في حال خطأ، تراجع عن كل ما سبق
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الطلب. حاول مرة أخرى.");
            return View(model);
        }
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
        }

        return RedirectToAction(nameof(Index));
    }

    private void SessionMsg(string MsgType, string Title, string Msg)
    {
        HttpContext.Session.SetString(Helper.MsgType, MsgType);
        HttpContext.Session.SetString(Helper.Title, Title);
        HttpContext.Session.SetString(Helper.Msg, Msg);
    }
}
