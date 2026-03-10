using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.DTOs.Auth;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;
using System.Security.Claims;

namespace Raksha.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            IOptions<JwtSettings> jwtSettings,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            // Single query to check both email and username
            var normalizedEmail = _userManager.NormalizeEmail(request.Email);
            var normalizedUserName = _userManager.NormalizeName(request.UserName);

            var existingUser = await _userManager.Users
                .Where(u => u.NormalizedEmail == normalizedEmail
                          || u.NormalizedUserName == normalizedUserName)
                .Select(u => new { u.NormalizedEmail, u.NormalizedUserName })
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return existingUser.NormalizedEmail == normalizedEmail
                    ? Result<AuthResponse>.Failure("User with this email already exists.")
                    : Result<AuthResponse>.Failure("User with this username already exists.");
            }

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
                return Result<AuthResponse>.Failure($"Failed to create user: {errors}");
            }

            // Assign default role — no need to look up the role entity
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign default role to user {UserId}", user.Id);
                return Result<AuthResponse>.Failure("Registration failed: could not assign default role.");
            }
            var roles = new List<string> { Roles.User };

            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.UserName!, roles);
            var refreshToken = CreateRefreshToken(user.Id);

            _dbContext.Set<RefreshToken>().Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, "Registration successful.");
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
                return Result<AuthResponse>.Failure("Invalid email or password.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Result<AuthResponse>.Failure("Account is locked out. Please try again later.");

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
                return Result<AuthResponse>.Failure("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.UserName!, roles);
            var refreshToken = CreateRefreshToken(user.Id);

            user.RefreshTokens ??= new List<RefreshToken>();
            user.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged in", user.Id);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, "Login successful.");
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return Result<AuthResponse>.Failure("Invalid access token.");

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Result<AuthResponse>.Failure("Invalid access token.");

            // Query specific token directly — avoids loading ALL user tokens
            var existingToken = await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

            if (existingToken == null)
                return Result<AuthResponse>.Failure("Invalid refresh token.");

            if (!existingToken.IsActive)
            {
                _logger.LogWarning("Attempted to use revoked/expired refresh token for user {UserId}", userId);
                return Result<AuthResponse>.Failure("Refresh token is expired or revoked.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result<AuthResponse>.Failure("User not found.");

            // Revoke old token
            existingToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.UserName!, roles);
            var newRefreshToken = CreateRefreshToken(user.Id);

            // Link old token to new for audit trail
            existingToken.ReplacedByToken = newRefreshToken.Token;

            _dbContext.Set<RefreshToken>().Add(newRefreshToken);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refresh token rotated for user {UserId}", userId);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, "Token refreshed successfully.");
        }

        public async Task<Result> RevokeTokenAsync(string refreshToken)
        {
            var token = await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                return Result.Failure("Invalid or already revoked token.");

            if (!token.IsActive)
                return Result.Failure("Invalid or already revoked token.");

            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);

            return Result.Success("Token revoked successfully.");
        }

        #region Private Helper Methods

        private RefreshToken CreateRefreshToken(Guid userId, string? ipAddress = null)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = _tokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress
            };
        }

        #endregion
    }
}
