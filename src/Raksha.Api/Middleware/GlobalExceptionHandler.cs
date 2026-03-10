using Microsoft.AspNetCore.Diagnostics;
using Raksha.Application.Models;

namespace Raksha.Api.Middleware
{
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception, "Unhandled exception: {Message}", exception.Message);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(
                Result.Failure("An unexpected error occurred."), cancellationToken);

            return true;
        }
    }
}
