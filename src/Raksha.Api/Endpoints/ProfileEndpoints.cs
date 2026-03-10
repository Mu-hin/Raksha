using Raksha.Application.DTOs.User;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using System.Security.Claims;

namespace Raksha.Api.Endpoints
{
    public static class ProfileEndpoints
    {
        public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/profile")
                .WithTags("Profile")
                .RequireAuthorization();

            group.MapGet("", async (ClaimsPrincipal user, IUserService userService, CancellationToken cancellationToken) =>
            {
                var userId = GetUserId(user);
                if (userId == null)
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                var result = await userService.GetByIdAsync(userId.Value, cancellationToken);

                if (!result.IsSuccess)
                    return Results.NotFound(result);

                return Results.Ok(result);
            })
            .WithName("GetProfile");

            group.MapPut("", async (UpdateUserProfileRequest request, ClaimsPrincipal user, IUserService userService, CancellationToken cancellationToken) =>
            {
                var userId = GetUserId(user);
                if (userId == null)
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                var result = await userService.UpdateProfileAsync(userId.Value, request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(result);

                return Results.Ok(result);
            })
            .WithName("UpdateProfile");

            group.MapPost("picture", async (IFormFile file, ClaimsPrincipal user, IFileService fileService, IUserService userService, CancellationToken cancellationToken) =>
            {
                var userId = GetUserId(user);
                if (userId == null)
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                using var stream = file.OpenReadStream();
                var saveResult = await fileService.SaveProfilePictureAsync(userId.Value, stream, file.FileName, cancellationToken);

                if (!saveResult.IsSuccess)
                    return Results.BadRequest(saveResult);

                // Update ImageKey in user profile
                var updateRequest = new UpdateUserProfileRequest { ImageKey = saveResult.Message };

                // Get current profile to preserve other fields
                var currentUser = await userService.GetByIdAsync(userId.Value, cancellationToken);
                if (currentUser.IsSuccess && currentUser.Data != null)
                {
                    updateRequest.FirstName = currentUser.Data.FirstName;
                    updateRequest.LastName = currentUser.Data.LastName;
                    updateRequest.ImageKey = saveResult.Message;
                }

                await userService.UpdateProfileAsync(userId.Value, updateRequest, cancellationToken);

                return Results.Ok(Result.Success("Profile picture uploaded successfully."));
            })
            .WithName("UploadProfilePicture")
            .DisableAntiforgery();

            group.MapGet("picture", async (ClaimsPrincipal user, IFileService fileService, CancellationToken cancellationToken) =>
            {
                var userId = GetUserId(user);
                if (userId == null)
                    return Results.Json(Result.Failure("Invalid token."), statusCode: StatusCodes.Status401Unauthorized);

                var fileResult = await fileService.GetProfilePictureAsync(userId.Value, cancellationToken);

                if (fileResult == null)
                    return Results.NotFound(Result.Failure("Profile picture not found."));

                return Results.File(fileResult.Value.FileStream!, fileResult.Value.ContentType!);
            })
            .WithName("DownloadProfilePicture");
        }

        private static Guid? GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }
    }
}
