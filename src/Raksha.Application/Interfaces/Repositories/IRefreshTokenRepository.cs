using Raksha.Domain.Entities;
using Raksha.Domain.Interfaces;

namespace Raksha.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository : ISqlRepository<RefreshToken, Guid>
    {
        Task<RefreshToken?> GetActiveByTokenAsync(string token, Guid userId);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    }
}
