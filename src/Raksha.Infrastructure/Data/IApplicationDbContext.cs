using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Raksha.Infrastructure.Data
{
    public interface IApplicationDbContext
    {
        ChangeTracker ChangeTracker { get; }
        DatabaseFacade Database { get; }
        DbSet<TEntity> DbSet<TEntity>() where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Saves only EntityState.Added entries.
        /// </summary>
        Task<int> MigrateChangesAsync(CancellationToken cancellationToken = default);
    }
}
