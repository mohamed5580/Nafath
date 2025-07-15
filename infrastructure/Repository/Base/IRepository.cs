using Domin.Entity;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Infrastructure.IRepository.Base
{
    public interface IRepository<T> where T : class
    {
        T FindById(int? ID);
        T SelectOne(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] navProperties
        );
        Task<T> SelectOneAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes
        );
        IEnumerable<T> FindAll();
        IEnumerable<T> FindAll(params string[] agers);
        Task<T> FindByIdasync(int id);

        Task<IEnumerable<T>> FindAllasync();
        Task<IEnumerable<T>> FindAllasync(params string[] includes);
        void AddOne(T entity);
        void UpdateOne(T entity);
        void DeleteOne(T entity);
        void AddList(T entity);

        Task AddOneAsync(T entity);
        Task UpdateOneAsync(T entity);
        Task DeleteOneAsync(T entity);
        void UpdateList(T entity);
        void DeleteList(T entity);
        IEnumerable<T> GetPaged(int pageNumber, int pageSize, out int totalItems);

        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize
        );
        // في IRepository<T>
        IQueryable<T> Query(params string[] includes);

        Task<IEnumerable<T>> FindByTypeIdAsync(int typeId, params string[] includes);
        Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> predicate);
        Task<bool> DoCheckout(CheckoutViewModel model);

        Task<int> CountAsync();
        Task<IEnumerable<T>> GetPaginatedAsync(
            int page,
            int pageSize,
            Func<IQueryable<T>, IQueryable<T>> include = null
        );
        // 1) إضافة دالة لإضافة مجموعة دفعة واحدة
        Task AddRangeAsync(IEnumerable<T> entities);

        // 2) دالة صريحة لحفظ التغييرات (Unit of Work)
        Task SaveAsync();

        // 3) (اختياري) فتح معاملة على مستوى الـ Repository/Context
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<(IEnumerable<T> Items, int TotalCount)> GetMyOrdersAsync(
        int pageNumber,
        int pageSize);

        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetPaginatedAsync(int page, int pageSize,
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        Task DeleteRangeAsync(IEnumerable<T> entities);

    }
}
