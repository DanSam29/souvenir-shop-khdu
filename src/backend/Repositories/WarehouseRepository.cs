using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly AppDbContext _context;

        public WarehouseRepository(AppDbContext context) => _context = context;

        public async Task<IncomingDocument?> GetIncomingByIdAsync(int id) =>
            await _context.IncomingDocuments
                .Include(d => d.Product)
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

        public async Task<IReadOnlyList<IncomingDocument>> GetIncomingListAsync() =>
            await _context.IncomingDocuments
                .AsNoTracking()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .OrderByDescending(d => d.DocumentDate)
                .ToListAsync();

        public async Task<OutgoingDocument?> GetOutgoingByIdAsync(int id) =>
            await _context.OutgoingDocuments
                .Include(d => d.Product)
                .Include(d => d.Order)
                .Include(d => d.Company)
                .Include(d => d.AppliedPromotion)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

        public async Task<IReadOnlyList<OutgoingDocument>> GetOutgoingListAsync() =>
            await _context.OutgoingDocuments
                .AsNoTracking()
                .Include(d => d.Product)
                .Include(d => d.Order)
                .OrderByDescending(d => d.DocumentDate)
                .ToListAsync();

        public async Task AddAsync(IncomingDocument document) =>
            await _context.IncomingDocuments.AddAsync(document);

        public async Task AddAsync(OutgoingDocument document) =>
            await _context.OutgoingDocuments.AddAsync(document);

        public void Update(IncomingDocument document) => _context.IncomingDocuments.Update(document);

        public void Update(OutgoingDocument document) => _context.OutgoingDocuments.Update(document);

        public void Remove(IncomingDocument document) => _context.IncomingDocuments.Remove(document);

        public void Remove(OutgoingDocument document)
        {
            // Позначаємо лише сам документ як видалений, не прикріплюючи граф навігацій:
            // пов'язані Order/Product можуть уже відслідковуватися в цьому ж контексті (конфлікт ключів).
            _context.Entry(document).State = EntityState.Deleted;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await _context.SaveChangesAsync(cancellationToken);
    }
}
