using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;

namespace Raksha.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            ApplicationDbContext dbContext,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<SessionService> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<Result> InvalidateAllSessionsAsync(Guid userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var activeTokens = await _dbContext.Set<RefreshToken>()
                    .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                if (activeTokens.Count == 0)
                {
                    await transaction.CommitAsync();
                    return Result.Success();
                }

                foreach (var token in activeTokens)
                    token.RevokedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                var jwtTokens = activeTokens
                    .Select(t => t.JwtToken)
                    .Where(j => !string.IsNullOrEmpty(j));

                var ttl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);
                await _tokenService.BlacklistTokensAsync(jwtTokens, ttl);

                await transaction.CommitAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to invalidate sessions for user {UserId}", userId);
                return Result.Failure("Failed to invalidate sessions.");
            }
        }
    }
}
