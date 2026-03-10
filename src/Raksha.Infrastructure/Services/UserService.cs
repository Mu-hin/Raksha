using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raksha.Application.DTOs.User;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using Raksha.Domain.Common;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
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

            return Result<UserResponse>.Success(MapToResponse(user, roles));
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

            return Result<PagedResult<UserResponse>>.Success(new PagedResult<UserResponse>
            {
                Items = userResponses,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<Result> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            var user = await _userManager.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result.Failure("User not found.");

            user.UserDetails.FullName = request.FullName;
            user.UserDetails.LastName = request.LastName;
            user.UserDetails.ImageKey = request.ImageKey;

            await _dbContext.SaveChangesAsync();

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

        #region Private Helpers

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
