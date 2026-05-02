using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OMS.Domain.Entities;
using OMS.Domain.Interfaces;
using OMS.Infrastructure.Data;

namespace OMS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity {
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context) {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync() {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string? includeProperties = null) {
        IQueryable<T> query = _dbSet;

        if (filter != null)
            query = query.Where(filter);

        if (!string.IsNullOrWhiteSpace(includeProperties)) {
            foreach (var prop in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                query = query.Include(prop.Trim());
        }

        var totalCount = await query.CountAsync();

        if (orderBy != null)
            query = orderBy(query);
        else
            query = query.OrderBy(e => e.Id);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(T entity) {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity) {
        _dbSet.Update(entity);
    }

    public void Delete(T entity) {
        _dbSet.Remove(entity);
    }
}
