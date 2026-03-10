using Raksha.Application.DTOs.Identity;
using Raksha.Application.Models;
using Raksha.Domain.Common;

namespace Raksha.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        // User lookup
        Task<IdentityUserDto?> FindByIdAsync(Guid userId, CancellationToken ct = default);
        Task<IdentityUserDto?> FindByEmailAsync(string email, CancellationToken ct = default);
        Task<IdentityUserDto?> FindByIdWithDetailsAsync(Guid userId, CancellationToken ct = default);

        // User creation
        Task<Result<IdentityUserDto>> CreateUserAsync(string email, string userName, string password, string firstName, string? lastName, CancellationToken ct = default);

        // Password operations
        Task<Result> CheckPasswordSignInAsync(Guid userId, string password, CancellationToken ct = default);
        Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

        // Role operations
        Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
        Task<Result> AddToRoleAsync(Guid userId, string role, CancellationToken ct = default);
        Task<Result> RemoveFromRoleAsync(Guid userId, string role, CancellationToken ct = default);
        Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken ct = default);
        Task<bool> RoleExistsAsync(string role, CancellationToken ct = default);

        // User updates
        Task<Result> UpdateUserStatusAsync(Guid userId, EntityStatus status, CancellationToken ct = default);
        Task<Result> UpdateProfileAsync(Guid userId, string firstName, string? lastName, string? imageKey, CancellationToken ct = default);

        // Duplicate checks
        Task<DuplicateCheckResult?> CheckDuplicateAsync(string email, string userName, CancellationToken ct = default);

        // Query (for user listing with filtering)
        Task<(List<IdentityUserDto> Users, int TotalCount)> GetFilteredUsersAsync(
            string? searchTerm, EntityStatus? status, string? role, int page, int pageSize, CancellationToken ct = default);
    }
}
