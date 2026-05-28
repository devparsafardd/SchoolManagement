using System.Linq.Expressions;

namespace SMS.Application.Common.Repositories;

/// <summary>
/// Repository عمومی برای CRUD های ساده
/// (برای منطق پیچیده مستقیماً از DbContext در سرویس‌ها استفاده می‌شود)
/// </summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Query(bool tracking = false);
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
