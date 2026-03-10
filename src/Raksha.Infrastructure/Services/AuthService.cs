using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raksha.Application.DTOs.Auth;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Raksha.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private const string defaultRoleName = "User";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext dbContext,
            IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists.");
            }

            var existingUserName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserName != null)
            {
                throw new InvalidOperationException("User with this username already exists.");
            }

            // Create new user
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.UserName,
                UserDetails = new UserDetails
                {
                    FullName = $"{request.FirstName} {request.LastName}".Trim(),
                    LastName = request.LastName
                },
                RefreshTokens = new List<RefreshToken>()
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            //assign to default role
            var defaultRole = await _roleManager.FindByNameAsync(defaultRoleName) ?? throw new InvalidOperationException("Default role not found");
            await _userManager.AddToRolesAsync(user, new List<string> { defaultRole.Name });

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateAccessToken(user, roles.ToList());
            var refreshToken = await GenerateRefreshTokenAsync(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                throw new InvalidOperationException("Account is locked out. Please try again later.");
            }

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateAccessToken(user, roles.ToList());
            var refreshToken = await GenerateRefreshTokenAsync(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Validate the expired access token (without validating lifetime)
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                throw new InvalidOperationException("Invalid access token.");
            }

            // Get user id from claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Invalid access token.");
            }

            // Find user
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            // Find the refresh token (using GUID)
            var refreshToken = user.RefreshTokens
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
            {
                throw new InvalidOperationException("Invalid refresh token.");
            }

            if (!refreshToken.IsActive)
            {
                throw new InvalidOperationException("Refresh token is expired or revoked.");
            }

            // Revoke the old refresh token
            refreshToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateAccessToken(user, roles.ToList());
            var newRefreshToken = await GenerateRefreshTokenAsync(user);

            // Link old token to new token for audit trail
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            // Find the refresh token across all users
            var token = await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
            {
                return false;
            }

            if (!token.IsActive)
            {
                return false;
            }

            // Revoke the token
            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        #region Private Helper Methods

        private string GenerateAccessToken(ApplicationUser user, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add role claims
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
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user, string? ipAddress = null)
        {
            // Generate a GUID-based refresh token
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(), // Using GUID instead of base64 string
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress
            };

            // Add to user's refresh tokens
            user.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Don't validate lifetime for expired tokens
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
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
