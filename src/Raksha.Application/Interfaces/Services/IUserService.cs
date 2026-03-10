using Raksha.Application.DTOs.User;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<Result<UserResponse>> GetByIdAsync(Guid userId, CancellationToken ct = default);
        Task<Result<PagedResult<UserResponse>>> GetAllAsync(UserFilterRequest filter, CancellationToken ct = default);
        Task<Result> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken ct = default);
        Task<Result> ActivateAsync(Guid userId, CancellationToken ct = default);
        Task<Result> DeactivateAsync(Guid userId, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid userId, CancellationToken ct = default);
        Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
        Task<Result> RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default);
        Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
        Task<Result> ForceLogoutAsync(Guid userId, CancellationToken ct = default);
    }
}
