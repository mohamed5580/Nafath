using Domin.Entity;
using Infrastructure.IRepository.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    public interface IOrderRepository : IRepository<Order>
    {
        // دالة مخصصة لجلب طلبات مستخدم معين مع كل التفاصيل
        Task<IEnumerable<Order>> GetUserOrdersWithDetailsAsync(string userId);

        // دالة مخصصة لجلب طلب واحد بتفاصيله الكاملة والتأكد من ملكيته
        Task<Order> GetOrderWithDetailsAsync(int orderId, string userId);

        // دالة مخصصة للأدمن لجلب أي طلب بتفاصيله
        Task<Order> AdminGetOrderWithDetailsAsync(int orderId);
        Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync();

    }
}
