using Domin.Entity;
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.IRepository.Base;
using Infrastructure.ViewModel; // For CheckoutViewModel if needed
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Infrastructure.IRepository
{
    public class MainRepository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public MainRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // --- تصحيح: يجب استقبال قائمة وليس عنصراً واحداً ---
        public void AddList(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentNullException(nameof(entities), "Entities cannot be null or empty.");

            _context.Set<T>().AddRange(entities);
            _context.SaveChanges();
            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
        }

        public void AddOne(T entity)
        {
            if (entity == null)
            {
                SessionMsg(Helper.Error, "خطأ", "البيانات فارغة");
                return;
            }
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
            SessionMsg(Helper.Success, ResourceWeb.lbSaveMsg, ResourceWeb.lbSaveMsg);
        }

        // Async implementations ...
        public async Task AddOneAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
            // SaveChanges غير موجود هنا ليتيح استخدامها داخل Transaction
        }

        // ... بقية دوال الحذف والتعديل (Delete/Update) تبقى كما هي ...

        public T FindById(int? ID)
        {
            // تصحيح: التحقق من ID 
            if (ID == null || ID <= 0) return null;
            return _context.Set<T>().Find(ID);
        }

        public async Task<T> FindByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        // تصحيح الاسم typo: FindByIdasync -> FindByIdAsync
        public async Task<T> FindByIdasync(int id) => await FindByIdAsync(id);

        public IEnumerable<T> FindAll() => _context.Set<T>().ToList();

        public async Task<IEnumerable<T>> FindAllasync() => await _context.Set<T>().ToListAsync();

        public IEnumerable<T> FindAll(params string[] includes)
        {
            var query = _context.Set<T>().AsQueryable();
            foreach (var include in includes)
                query = query.Include(include);
            return query.ToList();
        }

        public async Task<IEnumerable<T>> FindAllasync(params string[] includes)
        {
            var query = _context.Set<T>().AsQueryable();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.ToListAsync();
        }

        // --- تصحيح GetMyOrdersAsync: تم نقل المنطق لجعله آمناً ---
        // ملاحظة: الأفضل نقل هذه الدالة لـ OrderRepository، لكن لتصحيحها هنا:
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetMyOrdersAsync(int pageNumber, int pageSize)
        {
            // هذا الكود سيفشل إذا لم يكن T هو Order
            if (typeof(T) != typeof(Order))
                throw new InvalidOperationException("This method is only for Orders.");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return (new List<T>(), 0);

            // استخدام Set<Order> صراحة
            var query = _context.Set<Order>()
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate);

            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            // تحويل النوع بأمان
            return (items.Cast<T>(), total);
        }

        // Helpers
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        private void SessionMsg(string msgType, string title, string msg)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString(Helper.MsgType, msgType);
                session.SetString(Helper.Title, title);
                session.SetString(Helper.Msg, msg);
            }
        }

        // Implement other interface members...
        public void DeleteList(T entity) { _context.Set<T>().RemoveRange(entity); _context.SaveChanges(); } // هذا خطأ، RemoveRange يحتاج List
        public void DeleteOne(T entity) { _context.Set<T>().Remove(entity); _context.SaveChanges(); }
        public void UpdateOne(T entity) { _context.Set<T>().Update(entity); _context.SaveChanges(); }
        public void UpdateList(T entity) { _context.Set<T>().UpdateRange(entity); _context.SaveChanges(); } // نفس الخطأ يحتاج List

        // تصحيح دوال List في الواجهة يجب أن تأخذ IEnumerable
        // لكن بما أن الواجهة IRepository عندك تستقبل T في AddList، سأفترض أنك تمرر List<T> كـ Object؟
        // لا، يجب تعديل الواجهة IRepository لتستقبل IEnumerable<T>

        public IEnumerable<T> GetPaged(int pageNumber, int pageSize, out int totalItems)
        {
            var query = _context.Set<T>();
            totalItems = query.Count();
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Set<T>();
            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, count);
        }

        public IQueryable<T> Query(params string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var inc in includes) query = query.Include(inc);
            return query;
        }

        public async Task<IEnumerable<T>> FindByTypeIdAsync(int typeId, params string[] includes)
        {
            if (typeof(T) != typeof(Product)) return null;
            // Implementation logic...
            IQueryable<Product> q = _context.Set<Product>().Where(x => x.ProductTypeId == typeId);
            foreach (var inc in includes) q = q.Include(inc);
            var res = await q.ToListAsync();
            return res.Cast<T>();
        }

        public async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<bool> DoCheckout(CheckoutViewModel model) => throw new NotImplementedException();
        public async Task<int> CountAsync() => await _context.Set<T>().CountAsync();
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate) => await _context.Set<T>().CountAsync(predicate);

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();
            
        public async Task DeleteOneAsync(T entity) { _context.Set<T>().Remove(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateOneAsync(T entity) { _context.Set<T>().Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteRangeAsync(IEnumerable<T> entities) { _context.Set<T>().RemoveRange(entities); await _context.SaveChangesAsync(); }

        // غير موجودة في الـ Interface المرفق لكن تم ذكرها في Implementation
        public T SelectOne(Expression<Func<T, bool>> match) => _context.Set<T>().FirstOrDefault(match);
        public T SelectOne(Expression<Func<T, bool>> p, params Expression<Func<T, object>>[] inc)
        {
            var q = _context.Set<T>().AsQueryable();
            foreach (var i in inc) q = q.Include(i);
            return q.FirstOrDefault(p);
        }
        public async Task<T> SelectOneAsync(Expression<Func<T, bool>> p, params Expression<Func<T, object>>[] inc)
        {
            var q = _context.Set<T>().AsQueryable();
            foreach (var i in inc) q = q.Include(i);
            return await q.FirstOrDefaultAsync(p);
        }
        public async Task<IEnumerable<T>> GetPaginatedAsync(int page, int pageSize, Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> q = _context.Set<T>();
            if (include != null) q = include(q);
            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }
        public async Task<IEnumerable<T>> GetPaginatedAsync(int page, int pageSize, Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            var q = _context.Set<T>().Where(predicate);
            foreach (var i in includes) q = q.Include(i);
            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }
    }
}