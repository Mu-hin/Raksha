using Raksha.Application.Interfaces;
using Raksha.Application.Models;

namespace Raksha.Api.Middleware
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRedisCacheService redisCacheService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var authHeader = context.Request.Headers.Authorization.ToString();

                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var jwt = authHeader["Bearer ".Length..].Trim();

                    if (await redisCacheService.IsJwtBlacklistedAsync(jwt))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            Result.Failure("Your session has been invalidated. Please login again."));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
