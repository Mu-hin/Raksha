using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.DTOs.Auth;
using Raksha.Application.Interfaces.Repositories;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;
using System.Security.Claims;

namespace Raksha.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ISessionService _sessionService;
        private readonly IAuditService _auditService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IIdentityService identityService,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            ISessionService sessionService,
            IAuditService auditService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _identityService = identityService;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _sessionService = sessionService;
            _auditService = auditService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                return Result<AuthResponse>.Failure("A valid email is required.");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<AuthResponse>.Failure("Username is required.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return Result<AuthResponse>.Failure("Password is required and must be at least 6 characters.");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return Result<AuthResponse>.Failure("First name is required.");

            // Duplicate check
            var duplicate = await _identityService.CheckDuplicateAsync(request.Email, request.UserName, ct);
            if (duplicate != null)
            {
                return duplicate.IsDuplicateEmail
                    ? Result<AuthResponse>.Failure("User with this email already exists.")
                    : Result<AuthResponse>.Failure("User with this username already exists.");
            }

            // Create user
            var createResult = await _identityService.CreateUserAsync(
                request.Email, request.UserName, request.Password, request.FirstName, request.LastName, ct);

            if (!createResult.IsSuccess)
                return Result<AuthResponse>.Failure(createResult.Message);

            var userDto = createResult.Data!;

            // Assign default role
            var roleResult = await _identityService.AddToRoleAsync(userDto.Id, Roles.User, ct);
            if (!roleResult.IsSuccess)
            {
                _logger.LogWarning("Failed to assign default role to user {UserId}", userDto.Id);
                return Result<AuthResponse>.Failure("Registration failed: could not assign default role.");
            }

            var roles = new List<string> { Roles.User };

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userDto.Id, userDto.Email, userDto.UserName, roles);
            var refreshToken = CreateRefreshToken(userDto.Id, accessToken);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);
            await _refreshTokenRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} registered with email {Email}", userDto.Id, userDto.Email);

            return Result<AuthResponse>.Success(data: new AuthResponse
            {
                UserId = userDto.Id,
                Email = userDto.Email,
                UserName = userDto.UserName,
                Roles = roles,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, message: "Registration successful.");
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Email))
                return Result<AuthResponse>.Failure("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result<AuthResponse>.Failure("Password is required.");

            // Find user
            var userDto = await _identityService.FindByEmailAsync(request.Email, ct);
            if (userDto == null)
            {
                _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
                return Result<AuthResponse>.Failure("Invalid email or password.");
            }

            // Check user status
            if (userDto.Status == EntityStatus.Inactive)
                return Result<AuthResponse>.Failure("Your account has been deactivated. Please contact support.");

            if (userDto.Status == EntityStatus.Deleted)
                return Result<AuthResponse>.Failure("Invalid email or password.");

            // Check password
            var signInResult = await _identityService.CheckPasswordSignInAsync(userDto.Id, request.Password, ct);
            if (!signInResult.IsSuccess)
            {
                _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
                return Result<AuthResponse>.Failure(signInResult.Message);
            }

            // Generate tokens
            var roles = await _identityService.GetRolesAsync(userDto.Id, ct);
            var accessToken = _tokenService.GenerateAccessToken(userDto.Id, userDto.Email, userDto.UserName, roles);
            var refreshToken = CreateRefreshToken(userDto.Id, accessToken);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);
            await _refreshTokenRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged in", userDto.Id);

            return Result<AuthResponse>.Success(data: new AuthResponse
            {
                UserId = userDto.Id,
                Email = userDto.Email,
                UserName = userDto.UserName,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, message: "Login successful.");
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return Result<AuthResponse>.Failure("Access token is required.");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result<AuthResponse>.Failure("Refresh token is required.");

            // Validate expired access token
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return Result<AuthResponse>.Failure("Invalid access token.");

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Result<AuthResponse>.Failure("Invalid access token.");

            // Find existing refresh token
            var existingToken = await _refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, userId, ct);
            if (existingToken == null)
                return Result<AuthResponse>.Failure("Invalid refresh token.");

            if (!existingToken.IsActive)
            {
                _logger.LogWarning("Attempted to use revoked/expired refresh token for user {UserId}", userId);
                return Result<AuthResponse>.Failure("Refresh token is expired or revoked.");
            }

            // Find user
            var userDto = await _identityService.FindByIdAsync(userId, ct);
            if (userDto == null)
                return Result<AuthResponse>.Failure("User not found.");

            // Check user status
            if (userDto.Status == EntityStatus.Inactive || userDto.Status == EntityStatus.Deleted)
                return Result<AuthResponse>.Failure("Your account is no longer active.");

            // Revoke old token
            existingToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var roles = await _identityService.GetRolesAsync(userId, ct);
            var newAccessToken = _tokenService.GenerateAccessToken(userDto.Id, userDto.Email, userDto.UserName, roles);
            var newRefreshToken = CreateRefreshToken(userId, newAccessToken);

            // Link old token to new for audit trail
            existingToken.ReplacedByToken = newRefreshToken.Token;

            await _refreshTokenRepository.AddAsync(newRefreshToken, ct);
            await _refreshTokenRepository.SaveChangesAsync();

            _logger.LogInformation("Refresh token rotated for user {UserId}", userId);

            return Result<AuthResponse>.Success(data: new AuthResponse
            {
                UserId = userDto.Id,
                Email = userDto.Email,
                UserName = userDto.UserName,
                Roles = roles.ToList(),
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, message: "Token refreshed successfully.");
        }

        public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, ct);

            if (token == null)
                return Result.Failure("Invalid or already revoked token.");

            if (!token.IsActive)
                return Result.Failure("Invalid or already revoked token.");

            token.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);

            return Result.Success("Token revoked successfully.");
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return Result.Failure("Current password is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return Result.Failure("New password is required and must be at least 6 characters.");

            if (request.CurrentPassword == request.NewPassword)
                return Result.Failure("New password must be different from current password.");

            // Find user
            var userDto = await _identityService.FindByIdAsync(userId, ct);
            if (userDto == null)
                return Result.Failure("User not found.");

            // Change password via identity abstraction
            var changeResult = await _identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
            if (!changeResult.IsSuccess)
                return changeResult;

            // Invalidate all sessions via session abstraction
            var invalidateResult = await _sessionService.InvalidateAllSessionsAsync(userId, ct);
            if (!invalidateResult.IsSuccess)
                return invalidateResult;

            // Audit log
            await _auditService.LogAsync(userId, userDto.Email, "PasswordChange", "Password changed by user.", ct);

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return Result.Success("Password changed successfully. All sessions have been invalidated.");
        }

        #region Private Helpers

        private RefreshToken CreateRefreshToken(Guid userId, string accessToken, string? ipAddress = null)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                JwtToken = accessToken,
                Token = _tokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress
            };
        }

        #endregion
    }
}
