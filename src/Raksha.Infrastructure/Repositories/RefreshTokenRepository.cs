using Microsoft.EntityFrameworkCore;
using Raksha.Application.Interfaces.Repositories;
using Raksha.Domain.Entities;
using Raksha.Domain.Interfaces;
using Raksha.Infrastructure.Data;

namespace Raksha.Infrastructure.Repositories
{
    public class RefreshTokenRepository : SqlRepository<RefreshToken, Guid>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(IApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<RefreshToken?> GetActiveByTokenAsync(string token, Guid userId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(
                rt => rt.Token == token && rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow, ct);
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(rt => rt.Token == token, ct);
        }

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
        }
    }
}
