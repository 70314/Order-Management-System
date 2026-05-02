using System.Linq.Expressions;
using OMS.Domain.Entities;

namespace OMS.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity {
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string? includeProperties = null);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
