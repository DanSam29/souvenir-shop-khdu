using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface ICompanyRepository : IRepository<Company>
    {
        Task<IReadOnlyList<Company>> GetActiveAsync();
        Task<bool> NameExistsAsync(string name);
        Task<bool> EmailExistsAsync(string email);
    }
}
