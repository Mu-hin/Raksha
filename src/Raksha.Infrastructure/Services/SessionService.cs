using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.Interfaces.Repositories;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;

namespace Raksha.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<SessionService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<Result> InvalidateAllSessionsAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, ct);

                if (activeTokens.Count == 0)
                    return Result.Success();

                foreach (var token in activeTokens)
                    token.RevokedAt = DateTime.UtcNow;

                await _refreshTokenRepository.SaveChangesAsync();

                var jwtTokens = activeTokens
                    .Select(t => t.JwtToken)
                    .Where(j => !string.IsNullOrEmpty(j));

                var ttl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);
                await _tokenService.BlacklistTokensAsync(jwtTokens, ttl, ct);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate sessions for user {UserId}", userId);
                return Result.Failure("Failed to invalidate sessions.");
            }
        }
    }
}
