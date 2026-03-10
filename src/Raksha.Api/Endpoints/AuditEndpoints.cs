using Microsoft.AspNetCore.Authorization;
using Raksha.Application.DTOs.Audit;
using Raksha.Application.Interfaces.Services;
using Raksha.Domain.Common;

namespace Raksha.Api.Endpoints
{
    public static class AuditEndpoints
    {
        public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/audit")
                .WithTags("Audit")
                .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

            group.MapGet("password-changes", async ([AsParameters] AuditFilterRequest filter, IAuditService auditService, CancellationToken cancellationToken) =>
            {
                var result = await auditService.GetPasswordChangeHistoryAsync(filter, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetPasswordChangeHistory");

            group.MapGet("profile-updates", async ([AsParameters] AuditFilterRequest filter, IAuditService auditService, CancellationToken cancellationToken) =>
            {
                var result = await auditService.GetProfileUpdateHistoryAsync(filter, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetProfileUpdateHistory");
        }
    }
}
