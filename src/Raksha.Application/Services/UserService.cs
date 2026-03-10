using Microsoft.Extensions.Logging;
using Raksha.Application.DTOs.Identity;
using Raksha.Application.DTOs.User;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Common;

namespace Raksha.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IIdentityService _identityService;
        private readonly ISessionService _sessionService;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IIdentityService identityService,
            ISessionService sessionService,
            IAuditService auditService,
            ILogger<UserService> logger)
        {
            _identityService = identityService;
            _sessionService = sessionService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result<UserResponse>> GetByIdAsync(Guid userId)
        {
            var userDto = await _identityService.FindByIdWithDetailsAsync(userId);
            if (userDto == null)
                return Result<UserResponse>.Failure("User not found.");

            return Result<UserResponse>.Success(data: MapToResponse(userDto));
        }

        public async Task<Result<PagedResult<UserResponse>>> GetAllAsync(UserFilterRequest filter)
        {
            var (users, totalCount) = await _identityService.GetFilteredUsersAsync(
                filter.SearchTerm, filter.Status, filter.Role, filter.Page, filter.PageSize);

            var userResponses = users.Select(MapToResponse).ToList();

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
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return Result.Failure("First name is required.");

            // Get current profile for change tracking
            var userDto = await _identityService.FindByIdWithDetailsAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            // Track changes for audit
            var changes = new List<string>();
            if (userDto.FirstName != request.FirstName)
                changes.Add($"FirstName: '{userDto.FirstName}' → '{request.FirstName}'");
            if (userDto.LastName != request.LastName)
                changes.Add($"LastName: '{userDto.LastName}' → '{request.LastName}'");
            if (userDto.ImageKey != request.ImageKey)
                changes.Add("ImageKey updated");

            // Update via identity abstraction
            var result = await _identityService.UpdateProfileAsync(userId, request.FirstName, request.LastName, request.ImageKey);
            if (!result.IsSuccess)
                return result;

            // Audit log
            if (changes.Count > 0)
                await _auditService.LogAsync(userId, userDto.Email, "ProfileUpdate", string.Join("; ", changes));

            _logger.LogInformation("Profile updated for user {UserId}", userId);

            return Result.Success("Profile updated successfully.");
        }

        public async Task<Result> ActivateAsync(Guid userId)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var result = await _identityService.UpdateUserStatusAsync(userId, (int)EntityStatus.Active);
            if (!result.IsSuccess) return result;

            _logger.LogInformation("User {UserId} activated", userId);
            return Result.Success("User activated successfully.");
        }

        public async Task<Result> DeactivateAsync(Guid userId)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var result = await _identityService.UpdateUserStatusAsync(userId, (int)EntityStatus.Inactive);
            if (!result.IsSuccess) return result;

            _logger.LogInformation("User {UserId} deactivated", userId);
            return Result.Success("User deactivated successfully.");
        }

        public async Task<Result> DeleteAsync(Guid userId)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var result = await _identityService.UpdateUserStatusAsync(userId, (int)EntityStatus.Deleted);
            if (!result.IsSuccess) return result;

            _logger.LogInformation("User {UserId} soft-deleted", userId);
            return Result.Success("User deleted successfully.");
        }

        public async Task<Result> AssignRoleAsync(Guid userId, string role)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var roleExists = await _identityService.RoleExistsAsync(role);
            if (!roleExists)
                return Result.Failure($"Role '{role}' does not exist.");

            var isInRole = await _identityService.IsInRoleAsync(userId, role);
            if (isInRole)
                return Result.Failure($"User already has role '{role}'.");

            var result = await _identityService.AddToRoleAsync(userId, role);
            if (!result.IsSuccess) return result;

            _logger.LogInformation("Role '{Role}' assigned to user {UserId}", role, userId);
            return Result.Success($"Role '{role}' assigned successfully.");
        }

        public async Task<Result> RemoveRoleAsync(Guid userId, string role)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var isInRole = await _identityService.IsInRoleAsync(userId, role);
            if (!isInRole)
                return Result.Failure($"User does not have role '{role}'.");

            var result = await _identityService.RemoveFromRoleAsync(userId, role);
            if (!result.IsSuccess) return result;

            _logger.LogInformation("Role '{Role}' removed from user {UserId}", role, userId);
            return Result.Success($"Role '{role}' removed successfully.");
        }

        public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request)
        {
            // Validation
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

            // Duplicate check
            var duplicate = await _identityService.CheckDuplicateAsync(request.Email, request.UserName);
            if (duplicate != null)
            {
                return duplicate.IsDuplicateEmail
                    ? Result<UserResponse>.Failure("User with this email already exists.")
                    : Result<UserResponse>.Failure("User with this username already exists.");
            }

            // Create user
            var createResult = await _identityService.CreateUserAsync(
                request.Email, request.UserName, request.Password, request.FirstName, request.LastName);

            if (!createResult.IsSuccess)
                return Result<UserResponse>.Failure(createResult.Message);

            var userDto = createResult.Data!;

            // Assign role
            var roleResult = await _identityService.AddToRoleAsync(userDto.Id, request.Role);
            if (!roleResult.IsSuccess)
            {
                _logger.LogWarning("Failed to assign role '{Role}' to user {UserId}", request.Role, userDto.Id);
                return Result<UserResponse>.Failure("User created but failed to assign role.");
            }

            userDto.Roles = new List<string> { request.Role };

            _logger.LogInformation("User {UserId} created by admin with role '{Role}'", userDto.Id, request.Role);

            return Result<UserResponse>.Success(data: MapToResponse(userDto), message: "User created successfully.");
        }

        public async Task<Result> ForceLogoutAsync(Guid userId)
        {
            var userDto = await _identityService.FindByIdAsync(userId);
            if (userDto == null)
                return Result.Failure("User not found.");

            var invalidateResult = await _sessionService.InvalidateAllSessionsAsync(userId);
            if (!invalidateResult.IsSuccess)
                return invalidateResult;

            _logger.LogInformation("Admin force-logged out user {UserId}", userId);
            return Result.Success("User has been force-logged out. All sessions invalidated.");
        }

        #region Private Helpers

        private static UserResponse MapToResponse(IdentityUserDto userDto)
        {
            return new UserResponse
            {
                Id = userDto.Id,
                Email = userDto.Email,
                UserName = userDto.UserName,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName ?? string.Empty,
                ImageKey = userDto.ImageKey,
                Roles = userDto.Roles,
                Status = userDto.Status,
                CreatedAt = userDto.CreatedAt
            };
        }

        #endregion
    }
}
