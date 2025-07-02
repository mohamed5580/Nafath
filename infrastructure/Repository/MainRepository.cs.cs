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
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

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

        public async Task<IEnumerable<T>> FindAllAsync(params string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.ToListAsync();
        }

        public Task<IEnumerable<T>> FindAllasync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> FindAllasync(params string[] agers)
        {
            throw new NotImplementedException();
        }

        public T FindById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(id));

            return _context.Set<T>().Find(id)
                   ?? throw new KeyNotFoundException($"Entity with ID {id} not found.");
        }

        public T FindById(int? ID)
        {
            throw new NotImplementedException();
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

        public IEnumerable<T> GetPaged(int pageNumber, int pageSize, out int totalItems)
        {
            var query = _context.Set<T>();
            totalItems = query.Count();
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }
    }
}
