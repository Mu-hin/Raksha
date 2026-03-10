using Raksha.Application.DTOs.User;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<Result<UserResponse>> GetByIdAsync(Guid userId);
        Task<Result<PagedResult<UserResponse>>> GetAllAsync(UserFilterRequest filter);
        Task<Result> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request);
        Task<Result> ActivateAsync(Guid userId);
        Task<Result> DeactivateAsync(Guid userId);
        Task<Result> DeleteAsync(Guid userId);
        Task<Result> AssignRoleAsync(Guid userId, string role);
        Task<Result> RemoveRoleAsync(Guid userId, string role);
        Task<Result<UserResponse>> CreateAsync(CreateUserRequest request);
        Task<Result> ForceLogoutAsync(Guid userId);
    }
}
