using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<IReadOnlyList<User>> GetByRoleAsync(string role);
        Task<User?> GetWithCartAsync(int userId);
    }
}
