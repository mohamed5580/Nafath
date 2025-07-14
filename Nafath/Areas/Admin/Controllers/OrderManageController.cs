using Domin.Entity;             // Order, OrderItem
using Domin.Resource;
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

    public OrderManageController(
        IRepository<Order> orderRepo,
        IRepository<OrderItem> orderItemRepo,
        IRepository<Product> productRepo,
        UserManager<ApplicationUser> userManager)
        {
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

        // 1) فكّ ترميز الأصناف من الـ JSON
        model.Items = JsonConvert
            .DeserializeObject<List<CartItemDto>>(ItemsJson)
            ?? new List<CartItemDto>();

        if (!model.Items.Any())
        {
            ModelState.AddModelError("", "السلة فارغة.");
            return View(model);
        }

        // 2) أنشئ الـ Order
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now
        };
        await _orderRepo.AddOneAsync(order);

        // 3) أنشئ كل OrderItem
        foreach (var ci in model.Items)
        {
            var product = await _productRepo.FindByIdasync(ci.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("", $"المنتج {ci.ProductId} غير موجود.");
                return View(model);
            }

            var item = new OrderItem
            {
                OrderId = order.Id,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            };
            await _orderItemRepo.AddOneAsync(item);
        }

        // 4) كل إضافة باستخدام AddOneAsync تحقّق SaveChanges داخليًا
        return RedirectToAction("Confirmation", new { id = order.Id });
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


    private void SessionMsg(string MsgType, string Title, string Msg)
    {
        HttpContext.Session.SetString(Helper.MsgType, MsgType);
        HttpContext.Session.SetString(Helper.Title, Title);
        HttpContext.Session.SetString(Helper.Msg, Msg);
    }
}
