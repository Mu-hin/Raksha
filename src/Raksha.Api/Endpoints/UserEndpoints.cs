using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raksha.Application.DTOs.User;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Common;

namespace Raksha.Api.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/users")
                .WithTags("Users");

            group.MapPost("", async (CreateUserRequest request, IUserService userService) =>
            {
                var result = await userService.CreateAsync(request);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("CreateUser")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapGet("{id:guid}", async (Guid id, IUserService userService) =>
            {
                var result = await userService.GetByIdAsync(id);

                if (!result.IsSuccess)
                    return Results.NotFound(result);

                return Results.Ok(result);
            })
            .WithName("GetUserById")
            .RequireAuthorization();

            group.MapGet("", async ([AsParameters] UserFilterRequest filter, IUserService userService) =>
            {
                var result = await userService.GetAllAsync(filter);
                return Results.Ok(result);
            })
            .WithName("GetAllUsers")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapPut("{id:guid}/profile", async (Guid id, UpdateUserProfileRequest request, IUserService userService) =>
            {
                var result = await userService.UpdateProfileAsync(id, request);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("UpdateUserProfile")
            .RequireAuthorization();

            group.MapPut("{id:guid}/activate", async (Guid id, IUserService userService) =>
            {
                var result = await userService.ActivateAsync(id);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("ActivateUser")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapPut("{id:guid}/deactivate", async (Guid id, IUserService userService) =>
            {
                var result = await userService.DeactivateAsync(id);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("DeactivateUser")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapDelete("{id:guid}", async (Guid id, IUserService userService) =>
            {
                var result = await userService.DeleteAsync(id);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("DeleteUser")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapPost("{id:guid}/roles/{role}", async (Guid id, string role, IUserService userService) =>
            {
                var result = await userService.AssignRoleAsync(id, role);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("AssignRole")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapDelete("{id:guid}/roles/{role}", async (Guid id, string role, IUserService userService) =>
            {
                var result = await userService.RemoveRoleAsync(id, role);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("RemoveRole")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapPost("{id:guid}/force-logout", async (Guid id, IUserService userService) =>
            {
                var result = await userService.ForceLogoutAsync(id);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("ForceLogoutUser")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });
        }
    }
}
