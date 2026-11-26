using Microsoft.EntityFrameworkCore;
using HotelCatalog.Dal.Interfaces;
using HotelCatalog.Dal.Data;
using System.Linq.Expressions;
using Ardalis.Specification.EntityFrameworkCore;

namespace HotelCatalog.Dal.Repositories
{
    public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
    {
        protected readonly HotelCatalogDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(HotelCatalogDbContext context) : base(context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            return entity is not null && _context.Entry(entity).Property<bool>("IsDeleted").CurrentValue
                ? null
                : entity;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(entity => !EF.Property<bool>(entity, "IsDeleted")).ToListAsync(cancellationToken);
        }

        public new virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            _context.Entry(entity).Property<bool>("IsDeleted").CurrentValue = true;
            _dbSet.Update(entity);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(entity => !EF.Property<bool>(entity, "IsDeleted"))
                .Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(entity => !EF.Property<bool>(entity, "IsDeleted"))
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }
    }
}
