using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MyBankingApp.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // Read operations
        IQueryable<T> GetAll();
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);
        T GetById(Guid id);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);

        // Write operations
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Save
        int SaveChanges();
    }
}