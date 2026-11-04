using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KhduSouvenirShop.API.Data;

namespace KhduSouvenirShop.API.Repositories
{
    /// <summary>
    /// Реалізація <see cref="IUnitOfWork"/> на основі єдиного DbContext,
    /// що використовується всіма репозиторіями в межах запиту.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context) => _context = context;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
