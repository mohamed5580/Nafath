using Domin.Entity;
using Infrastructure.IRepository.Base;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository.Base
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Product> Products { get; }
        IRepository<ProductType> ProductTypes { get; }
        IOrderRepository Orders { get; }
        IRepository<OrderItem> OrderItems { get; }
        Task<int> CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
