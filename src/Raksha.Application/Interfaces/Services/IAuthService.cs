using Raksha.Application.DTOs.Auth;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Result> RevokeTokenAsync(string refreshToken);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
