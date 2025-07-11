using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        // 1) أنشئ كيان الـ Order
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now
        };

        // 2) أضفه وحفظه أولًا ليُولَّد الـ Id
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        // الآن order.Id يحتوي على القيمة الصحيحة من قاعدة البيانات

        // 3) أضف كل عنصر طلب باستخدام order.Id
        foreach (var ci in request.Items)
        {
            var product = await _context.Products.FindAsync(ci.ProductId);
            if (product == null)
                return BadRequest($"ProductId {ci.ProductId} not found");

            var item = new OrderItem
            {
                OrderId = order.Id,         // ← رقم الطلب الصحيح
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            };
            _context.OrderItems.Add(item);
        }

        // 4) احفظ كل عناصر الطلب
        await _context.SaveChangesAsync();

        return Ok(new { orderId = order.Id });
    }
}

    // لفك التجميع:
    public class CheckoutDto
{
    public List<CartItemDto> Items { get; set; }
}
