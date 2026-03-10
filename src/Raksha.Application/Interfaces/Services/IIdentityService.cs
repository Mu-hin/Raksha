using Raksha.Application.DTOs.Identity;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        // User lookup
        Task<IdentityUserDto?> FindByIdAsync(Guid userId);
        Task<IdentityUserDto?> FindByEmailAsync(string email);
        Task<IdentityUserDto?> FindByIdWithDetailsAsync(Guid userId);

        // User creation
        Task<Result<IdentityUserDto>> CreateUserAsync(string email, string userName, string password, string firstName, string? lastName);

        // Password operations
        Task<Result> CheckPasswordSignInAsync(Guid userId, string password);
        Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        // Role operations
        Task<IList<string>> GetRolesAsync(Guid userId);
        Task<Result> AddToRoleAsync(Guid userId, string role);
        Task<Result> RemoveFromRoleAsync(Guid userId, string role);
        Task<bool> IsInRoleAsync(Guid userId, string role);
        Task<bool> RoleExistsAsync(string role);

        // User updates
        Task<Result> UpdateUserStatusAsync(Guid userId, int status);
        Task<Result> UpdateProfileAsync(Guid userId, string firstName, string? lastName, string? imageKey);

        // Duplicate checks
        Task<DuplicateCheckResult?> CheckDuplicateAsync(string email, string userName);

        // Query (for user listing with filtering)
        Task<(List<IdentityUserDto> Users, int TotalCount)> GetFilteredUsersAsync(
            string? searchTerm, int? status, string? role, int page, int pageSize);
    }
}
