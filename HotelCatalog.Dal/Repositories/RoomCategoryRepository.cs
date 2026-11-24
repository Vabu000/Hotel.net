using Microsoft.EntityFrameworkCore;
using HotelCatalog.Dal.Data;
using HotelCatalog.Dal.Interfaces;
using HotelCatalog.Domain.Entities;

namespace HotelCatalog.Dal.Repositories
{
    public class RoomCategoryRepository : Repository<RoomCategory>, IRoomCategoryRepository
    {
        public RoomCategoryRepository(HotelCatalogDbContext context) : base(context)
        {
        }

        public async Task<RoomCategory?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(category => category.Details)
                .FirstOrDefaultAsync(category => category.CategoryId == categoryId, cancellationToken);
        }

        public override async Task<IEnumerable<RoomCategory>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(category => category.Details)
                .ToListAsync(cancellationToken);
        }

        public async Task LoadServicesAsync(RoomCategory category, CancellationToken cancellationToken = default)
        {
            await _context.Entry(category)
                .Collection(c => c.RoomServices)
                .Query()
                .Include(rs => rs.Service)
                .LoadAsync(cancellationToken);
        }

        public async Task<IEnumerable<Service>> GetServicesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            return await _context.RoomServices
                .Where(rs => rs.CategoryId == categoryId)
                .Select(rs => rs.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<RoomCategory>> GetCategoriesWithServiceCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(category => category.Details)
                .Include(rc => rc.RoomServices)
                .OrderByDescending(rc => rc.RoomServices.Count)
                .ToListAsync(cancellationToken);
        }
    }
}