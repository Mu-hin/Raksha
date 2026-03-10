using Raksha.Application.DTOs.Auth;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using System.Security.Claims;

namespace Raksha.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/auth").WithTags("Auth");

            group.MapPost("register", async (RegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RegisterAsync(request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("Register")
            .AllowAnonymous();

            group.MapPost("login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.LoginAsync(request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);

                return Results.Ok(result);
            })
            .WithName("Login")
            .AllowAnonymous();

            group.MapPost("refresh-token", async (RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RefreshTokenAsync(request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);

                return Results.Ok(result);
            })
            .WithName("RefreshToken")
            .AllowAnonymous();

            group.MapPost("revoke-token", async (RevokeTokenRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("RevokeToken")
            .RequireAuthorization();

            group.MapPost("change-password", async (ChangePasswordRequest request, ClaimsPrincipal user, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                var result = await authService.ChangePasswordAsync(parsedUserId, request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("ChangePassword")
            .RequireAuthorization();

            group.MapGet("me", (ClaimsPrincipal user) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                var userName = user.FindFirst(ClaimTypes.Name)?.Value;
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                if (string.IsNullOrEmpty(userId))
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                var profile = new UserProfileResponse
                {
                    UserId = Guid.Parse(userId),
                    Email = email ?? string.Empty,
                    UserName = userName ?? string.Empty,
                    Roles = roles
                };

                return Results.Ok(Result<UserProfileResponse>.Success(data: profile));
            })
            .WithName("GetCurrentUser")
            .RequireAuthorization();
        }
    }
}
