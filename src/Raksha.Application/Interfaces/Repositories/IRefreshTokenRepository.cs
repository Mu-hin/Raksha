using Raksha.Domain.Entities;
using Raksha.Domain.Interfaces;

namespace Raksha.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository : ISqlRepository<RefreshToken, Guid>
    {
        Task<RefreshToken?> GetActiveByTokenAsync(string token, Guid userId, CancellationToken ct = default);
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    }
}
