using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository.Base;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IRepository<Product> Products { get; private set; }
        public IRepository<ProductType> ProductTypes { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public IRepository<OrderItem> OrderItems { get; private set; }

        public UnitOfWork(ApplicationDbContext context, MainRepository<Product> products,
                      MainRepository<ProductType> productTypes,
                      OrderRepository orders,
                      MainRepository<OrderItem> orderItems)
        {
            _context = context;
            Products = products;
            ProductTypes = productTypes;
            Orders = orders;
            OrderItems = orderItems;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
