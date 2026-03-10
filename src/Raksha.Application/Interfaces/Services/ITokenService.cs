using System.Security.Claims;

namespace Raksha.Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, string email, string userName, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        // JWT Blacklist operations
        Task BlacklistTokensAsync(IEnumerable<string> jwtTokens, TimeSpan ttl, CancellationToken ct = default);
        Task<bool> IsTokenBlacklistedAsync(string jwtToken, CancellationToken ct = default);
    }
}
