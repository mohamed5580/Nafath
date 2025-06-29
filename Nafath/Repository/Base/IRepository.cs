using System.Linq.Expressions;

namespace Nafath.Repository.Base
{
    public interface IRepository<T> where T : class
    {
        T FindById(int? ID);
        T SelectOne(Expression<Func<T, bool>> match);
        IEnumerable<T> FindAll();
        IEnumerable<T> FindAll(params string[] agers);
        Task<T> FindByIdasync(int id);

        Task<IEnumerable<T>> FindAllasync();
        Task<IEnumerable<T>> FindAllasync(params string[] agers);
        void AddOne(T entity);
        void UpdateOne(T entity);
        void DeleteOne(T entity);
        void AddList(T entity);
        void UpdateList(T entity);
        void DeleteList(T entity);


    }
}
