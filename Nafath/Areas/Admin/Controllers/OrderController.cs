using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Infrastructure.Models;  // CartItemDto
using Domin.Entity;             // Order, OrderItem

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Area("Admin")]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        if (request?.Items == null || !request.Items.Any())
            return BadRequest("السلة فارغة.");

        // احصل على معرف المستخدم الحالي
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // أنشئ كيان الطلب
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now
        };

        // أضف العناصر
        foreach (var ci in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                ChairId = ci.ChairId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // أرجع رقم الطلب
        return Json(new { orderId = order.Id });
    }
}

// لفك التجميع:
public class CheckoutDto
{
    public List<CartItemDto> Items { get; set; }
}
