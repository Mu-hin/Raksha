using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.DTOs.User;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;
        private readonly IRedisCacheService _redisCacheService;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IOptions<JwtSettings> jwtSettings,
            IRedisCacheService redisCacheService,
            IAuditService auditService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _redisCacheService = redisCacheService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result<UserResponse>> GetByIdAsync(Guid userId)
        {
            var user = await _userManager.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result<UserResponse>.Failure("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return Result<UserResponse>.Success(data: MapToResponse(user, roles));
        }

        public async Task<Result<PagedResult<UserResponse>>> GetAllAsync(UserFilterRequest filter)
        {
            var query = _userManager.Users
                .Include(u => u.UserDetails)
                .AsQueryable();

            // Filter by status
            if (filter.Status.HasValue)
                query = query.Where(u => u.Status == filter.Status.Value);
            else
                query = query.Where(u => u.Status != (int)EntityStatus.Deleted);

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(term) ||
                    u.UserName!.ToLower().Contains(term) ||
                    u.UserDetails.FullName.ToLower().Contains(term));
            }

            // Filter by role
            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.NormalizedName == filter.Role.ToUpperInvariant());

                if (role != null)
                {
                    var userIdsInRole = _dbContext.UserRoles
                        .Where(ur => ur.RoleId == role.Id)
                        .Select(ur => ur.UserId);

                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Load roles for each user
            var userResponses = new List<UserResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userResponses.Add(MapToResponse(user, roles));
            }

            return Result<PagedResult<UserResponse>>.Success(data: new PagedResult<UserResponse>
            {
                Items = userResponses,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<Result> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure("Full name is required.");

            var user = await _userManager.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result.Failure("User not found.");

            var changes = new List<string>();
            if (user.UserDetails.FullName != request.FullName)
                changes.Add($"FullName: '{user.UserDetails.FullName}' → '{request.FullName}'");
            if (user.UserDetails.LastName != request.LastName)
                changes.Add($"LastName: '{user.UserDetails.LastName}' → '{request.LastName}'");
            if (user.UserDetails.ImageKey != request.ImageKey)
                changes.Add($"ImageKey updated");

            user.UserDetails.FullName = request.FullName;
            user.UserDetails.LastName = request.LastName;
            user.UserDetails.ImageKey = request.ImageKey;

            await _dbContext.SaveChangesAsync();

            if (changes.Count > 0)
                await _auditService.LogAsync(userId, user.Email!, "ProfileUpdate", string.Join("; ", changes));

            _logger.LogInformation("Profile updated for user {UserId}", userId);

            return Result.Success("Profile updated successfully.");
        }

        public async Task<Result> ActivateAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            user.Status = (int)EntityStatus.Active;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} activated", userId);

            return Result.Success("User activated successfully.");
        }

        public async Task<Result> DeactivateAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            user.Status = (int)EntityStatus.Inactive;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} deactivated", userId);

            return Result.Success("User deactivated successfully.");
        }

        public async Task<Result> DeleteAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            user.Status = (int)EntityStatus.Deleted;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} soft-deleted", userId);

            return Result.Success("User deleted successfully.");
        }

        public async Task<Result> AssignRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            var roleExists = await _dbContext.Roles
                .AnyAsync(r => r.NormalizedName == role.ToUpperInvariant());

            if (!roleExists)
                return Result.Failure($"Role '{role}' does not exist.");

            var isInRole = await _userManager.IsInRoleAsync(user, role);
            if (isInRole)
                return Result.Failure($"User already has role '{role}'.");

            await _userManager.AddToRoleAsync(user, role);

            _logger.LogInformation("Role '{Role}' assigned to user {UserId}", role, userId);

            return Result.Success($"Role '{role}' assigned successfully.");
        }

        public async Task<Result> RemoveRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            var isInRole = await _userManager.IsInRoleAsync(user, role);
            if (!isInRole)
                return Result.Failure($"User does not have role '{role}'.");

            await _userManager.RemoveFromRoleAsync(user, role);

            _logger.LogInformation("Role '{Role}' removed from user {UserId}", role, userId);

            return Result.Success($"Role '{role}' removed successfully.");
        }

        public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                return Result<UserResponse>.Failure("A valid email is required.");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<UserResponse>.Failure("Username is required.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return Result<UserResponse>.Failure("Password is required and must be at least 6 characters.");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return Result<UserResponse>.Failure("First name is required.");

            if (string.IsNullOrWhiteSpace(request.Role) ||
                (request.Role != Roles.Admin && request.Role != Roles.User))
                return Result<UserResponse>.Failure("Role must be 'Admin' or 'User'.");

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
                    ? Result<UserResponse>.Failure("User with this email already exists.")
                    : Result<UserResponse>.Failure("User with this username already exists.");
            }

            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.UserName,
                EmailConfirmed = true,
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
                return Result<UserResponse>.Failure($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign role '{Role}' to user {UserId}", request.Role, user.Id);
                return Result<UserResponse>.Failure("User created but failed to assign role.");
            }

            var roles = new List<string> { request.Role };
            _logger.LogInformation("User {UserId} created by admin with role '{Role}'", user.Id, request.Role);

            return Result<UserResponse>.Success(data: MapToResponse(user, roles), message: "User created successfully.");
        }

        public async Task<Result> ForceLogoutAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            var invalidateResult = await InvalidateUserSessionsAsync(userId);
            if (!invalidateResult.IsSuccess)
                return invalidateResult;

            _logger.LogInformation("Admin force-logged out user {UserId}", userId);
            return Result.Success("User has been force-logged out. All sessions invalidated.");
        }

        #region Private Helpers

        private async Task<Result> InvalidateUserSessionsAsync(Guid userId)
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
                await _redisCacheService.BlacklistJwtTokensAsync(jwtTokens, ttl);

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

        private static UserResponse MapToResponse(ApplicationUser user, IList<string> roles)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FullName = user.UserDetails?.FullName ?? string.Empty,
                LastName = user.UserDetails?.LastName ?? string.Empty,
                ImageKey = user.UserDetails?.ImageKey,
                Roles = roles.ToList(),
                Status = user.Status,
                CreatedAt = user.CreatedAt
            };
        }

        #endregion
    }
}
