using Domin.Entity;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    // يرث من MainRepository ليستفيد من الدوال البسيطة، وينفذ واجهته الخاصة
    public class OrderRepository : MainRepository<Order>, IOrderRepository
    {
        private readonly ApplicationDbContext _dbContext;


        public OrderRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
            : base(context, httpContextAccessor)
        {
            _dbContext = context;
        }
        public async Task<IEnumerable<Order>> GetUserOrdersWithDetailsAsync(string userId)
        {
            return await _dbContext.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderWithDetailsAsync(int orderId, string userId)
        {
            return await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public async Task<Order> AdminGetOrderWithDetailsAsync(int orderId)
        {
            return await _dbContext.Orders
               .Include(o => o.User) // تصحيح: استخدام User بدلاً من UserId
               .Include(o => o.OrderItems)
               .ThenInclude(oi => oi.Product)
               .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync()
        {
            return await _dbContext.Orders
                .Include(o => o.User) // تصحيح: استخدام User بدلاً من UserId
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

    }
}
