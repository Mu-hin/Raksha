using Raksha.Application.DTOs.Auth;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
        Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    }
}
