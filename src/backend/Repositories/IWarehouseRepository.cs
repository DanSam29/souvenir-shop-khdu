using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IWarehouseRepository
    {
        Task<IncomingDocument?> GetIncomingByIdAsync(int id);
        Task<IReadOnlyList<IncomingDocument>> GetIncomingListAsync();
        Task<OutgoingDocument?> GetOutgoingByIdAsync(int id);
        Task<IReadOnlyList<OutgoingDocument>> GetOutgoingListAsync();
        Task AddAsync(IncomingDocument document);
        Task AddAsync(OutgoingDocument document);
        void Update(IncomingDocument document);
        void Update(OutgoingDocument document);
        void Remove(IncomingDocument document);
        void Remove(OutgoingDocument document);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
