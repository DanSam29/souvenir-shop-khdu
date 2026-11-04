using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        public CompanyRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Company>> GetActiveAsync() =>
            await DbSet.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        public async Task<bool> NameExistsAsync(string name) =>
            await DbSet.AnyAsync(c => c.Name == name);

        public async Task<bool> EmailExistsAsync(string email) =>
            await DbSet.AnyAsync(c => c.Email == email);
    }
}
