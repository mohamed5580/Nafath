using Domin.Entity;
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.IRepository.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;

namespace Infrastructure.IRepository
{

    public class MainRepository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        // Correct constructor parameter names to avoid shadowing
        public MainRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void AddList(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            _context.Set<T>().AddRange(entity);
            _context.SaveChanges();

            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
        }

        public void AddOne(T entity)
        {
            if (entity == null)
                SessionMsg(Helper.Error, "خطأ", "الرجاء ملء جميع الحقول المطلوبة");


            _context.Set<T>().Add(entity);
            _context.SaveChanges();
           
            SessionMsg(Helper.Success, ResourceWeb.lbSaveMsg, ResourceWeb.lbSaveMsg);
        }

        public void DeleteList(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            _context.Set<T>().RemoveRange(entity);
            _context.SaveChanges();

            SessionMsg(Helper.Success, ResourceWeb.lbDeleteMsg, ResourceWeb.lbDeleteMsg);
        }

        public void DeleteOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            _context.Set<T>().Remove(entity);
            _context.SaveChanges();

            SessionMsg(Helper.Success, ResourceWeb.lbDeleteMsg, ResourceWeb.lbDeleteMsg);
        }

        public IEnumerable<T> FindAll()
        {
            // _context is now guaranteed non-null
            return _context.Set<T>().ToList();
        }

        public IEnumerable<T> FindAll(params string[] includes)
        {
            var query = _context.Set<T>().AsQueryable();
            foreach (var include in includes)
                query = query.Include(include);
            return query.ToList();
        }

        public async Task<IEnumerable<T>> FindAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

     

       

        public T FindById(int? ID)
        {
            if (ID <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(ID));

            return _context.Set<T>().Find(ID)
                   ?? throw new KeyNotFoundException($"Entity with ID {ID} not found.");
        }

        

        public async Task<T> FindByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(id));

            return await _context.Set<T>().FindAsync(id)
                   ?? throw new KeyNotFoundException($"Entity with ID {id} not found.");
        }

        public async Task<T> FindByIdasync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(id));

            return await _context.Set<T>().FindAsync(id)
                   ?? throw new KeyNotFoundException($"Entity with ID {id} not found.");
        }

        public T SelectOne(Expression<Func<T, bool>> match)
        {
            return _context.Set<T>().FirstOrDefault(match)
                   ?? throw new KeyNotFoundException("No matching entity found.");
        }

        public void UpdateList(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            _context.Set<T>().UpdateRange(entity);
            _context.SaveChanges();

            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
        }

        public void UpdateOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            _context.Set<T>().Update(entity);
            _context.SaveChanges();

            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
        }

    
      

       

        public IEnumerable<T> GetPaged(int pageNumber, int pageSize, out int totalItems)
        {
            var query = _context.Set<T>();
            totalItems = query.Count();
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Set<T>().AsQueryable();
            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public T SelectOne(
     Expression<Func<T, bool>> predicate,
     params Expression<Func<T, object>>[] includes
 )
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var inc in includes)
                query = query.Include(inc);
            return query.FirstOrDefault(predicate);
        }

        public async Task<T> SelectOneAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes
        )
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var inc in includes)
                query = query.Include(inc);
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> FindAllasync()
        {
            return await _context.Set<T>().ToListAsync();
        }


        public async Task<IEnumerable<T>> FindAllasync(params string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.ToListAsync();
        }

        public async Task AddOneAsync(T entity)
        {
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync();
            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);

        }

        public async Task UpdateOneAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);

        }

        public async Task DeleteOneAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);

        }
        // Session helper using IHttpContextAccessor
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

        public IEnumerable<T> FindByCondition(Expression<Func<T, bool>> predicate)
        {
            return _context.Set<T>().Where(predicate).ToList();
        }

        public async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindByTypeIdAsync(int typeId, params string[] includes)
        {
            if (typeof(T) != typeof(Product))
                throw new InvalidOperationException("This method can only be used with Product.");

            IQueryable<Product> query = _context.Products;

            foreach (var include in includes)
                query = query.Include(include);

            var products = await query.Where(p => p.ProductTypeId == typeId).ToListAsync();

            return products.Cast<T>(); 
        }

        public IQueryable<T> Query(params string[] includes)
        {
            IQueryable<T> q = _context.Set<T>();
            foreach (var inc in includes)
                q = q.Include(inc);
            return q;
        }
    }
}
