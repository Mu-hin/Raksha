using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Raksha.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<TokenService> _logger;

        private const string BlacklistPrefix = "blacklist:jwt:";

        private const string BlacklistLuaScript =
            @"local ttl = tonumber(ARGV[1])
              for i = 1, #KEYS do
                  redis.call('SETEX', KEYS[i], ttl, '1')
              end
              return #KEYS";

        public TokenService(IOptions<JwtSettings> jwtSettings, IRedisCacheService redisCacheService, ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email, string userName, IList<string> roles)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        #region JWT Blacklist Operations

        public async Task BlacklistTokensAsync(IEnumerable<string> jwtTokens, TimeSpan ttl, CancellationToken ct = default)
        {
            var keys = jwtTokens
                .Select(token => $"{BlacklistPrefix}{HashJwt(token)}")
                .ToArray();

            if (keys.Length == 0)
                return;

            var ttlSeconds = ((int)ttl.TotalSeconds).ToString();
            await _redisCacheService.ExecuteLuaScriptAsync(BlacklistLuaScript, keys, new[] { ttlSeconds }, ct);
        }

        public async Task<bool> IsTokenBlacklistedAsync(string jwtToken, CancellationToken ct = default)
        {
            var key = $"{BlacklistPrefix}{HashJwt(jwtToken)}";
            return await _redisCacheService.ExistsAsync(key, ct);
        }

        private static string HashJwt(string jwt)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(jwt));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion
    }
}
