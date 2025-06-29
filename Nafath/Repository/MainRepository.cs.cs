using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nafath.Data;
using Nafath.Repository.Base;

namespace Nafath.Repository
{
    public class MainRepository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        public MainRepository(ApplicationDbContext _context)
        {
            this._context = _context;
        }

        public void AddList(T entity)
        {

            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().AddRange(entity);
            _context.SaveChanges();
        }

        public void AddOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public void DeleteList(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().RemoveRange(entity);

            _context.SaveChanges();
        }

        public void DeleteOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Remove(entity);

            _context.SaveChanges();
        }

        public IEnumerable<T> FindAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> FindAll(params string[] agers)
        {
            throw new NotImplementedException();
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
            return _context.Set<T>().Find(id) ?? throw new KeyNotFoundException($"Entity with ID {id} not found.");
        }

        public async Task<T> FindByIdasync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            if (_context.Set<T>() == null)
                throw new InvalidOperationException("The DbSet is not initialized.");

            return await _context.Set<T>().FindAsync(id);
        }

        public T SelectOne(Expression<Func<T, bool>> match)
        {
            throw new NotImplementedException();
        }

        public void UpdateList(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().UpdateRange(entity);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("An error occurred while updating the entity.", ex);
            }
        }

        public void UpdateOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Update(entity);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("An error occurred while updating the entity.", ex);
            }
        }

        void IRepository<T>.AddOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Add(entity);
            _context.SaveChanges();

        }

        void IRepository<T>.DeleteOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Remove(entity);

            _context.SaveChanges();
        }

        T IRepository<T>.FindById(int? ID)
        {
            if (ID == null || ID <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(ID));
            var entity = _context.Set<T>().Find(ID);
            if (entity == null)
                throw new KeyNotFoundException($"Entity with ID {ID} not found.");

            return entity;
        }

        IEnumerable<T> IRepository<T>.FindAll()
        {
            return _context.Set<T>().ToList() ?? throw new InvalidOperationException("No entities found.");

        }



        void IRepository<T>.UpdateOne(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            _context.Set<T>().Update(entity);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("An error occurred while updating the entity.", ex);
            }
        }

        
    }

}
