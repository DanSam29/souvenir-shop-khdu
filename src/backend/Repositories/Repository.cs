using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;

namespace KhduSouvenirShop.API.Repositories
{
    /// <summary>
    /// Узагальнена реалізація репозиторію на основі DbSet.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<T> DbSet;

        public Repository(AppDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

        public virtual async Task<IReadOnlyList<T>> GetAllAsync() =>
            await DbSet.AsNoTracking().ToListAsync();

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await DbSet.AsNoTracking().Where(predicate).ToListAsync();

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
            await DbSet.FirstOrDefaultAsync(predicate);

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) =>
            await DbSet.AnyAsync(predicate);

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
            predicate is null ? await DbSet.CountAsync() : await DbSet.CountAsync(predicate);

        public virtual IQueryable<T> Query() => DbSet.AsQueryable();

        public virtual async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

        public virtual async Task AddRangeAsync(IEnumerable<T> entities) => await DbSet.AddRangeAsync(entities);

        public virtual void Update(T entity) => DbSet.Update(entity);

        public virtual void Remove(T entity) => DbSet.Remove(entity);

        public virtual void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await Context.SaveChangesAsync(cancellationToken);
    }
}
