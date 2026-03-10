using Raksha.Application.DTOs.Auth;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using System.Security.Claims;

namespace Raksha.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/auth").WithTags("Auth");

            group.MapPost("register", async (RegisterRequest request, IAuthService authService) =>
            {
                var result = await authService.RegisterAsync(request);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("Register")
            .AllowAnonymous()
            .Produces<Result<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest);

            group.MapPost("login", async (LoginRequest request, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(request);

                if (!result.IsSuccess)
                    return Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);

                return Results.Ok(result);
            })
            .WithName("Login")
            .AllowAnonymous()
            .Produces<Result<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status401Unauthorized);

            group.MapPost("refresh-token", async (RefreshTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RefreshTokenAsync(request);

                if (!result.IsSuccess)
                    return Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);

                return Results.Ok(result);
            })
            .WithName("RefreshToken")
            .AllowAnonymous()
            .Produces<Result<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status401Unauthorized);

            group.MapPost("revoke-token", async (RevokeTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RevokeTokenAsync(request.RefreshToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("RevokeToken")
            .RequireAuthorization()
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest);

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

                return Results.Ok(Result<UserProfileResponse>.Success(profile));
            })
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .Produces<Result<UserProfileResponse>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status401Unauthorized);
        }
    }
}
