using Microsoft.EntityFrameworkCore.Storage;

namespace KhduSouvenirShop.API.Repositories
{
    /// <summary>
    /// Єдина точка координації змін даних: збереження змін та керування транзакціями
    /// для операцій, що охоплюють кілька сутностей.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
