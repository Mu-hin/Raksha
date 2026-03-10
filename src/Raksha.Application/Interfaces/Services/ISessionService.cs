using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface ISessionService
    {
        Task<Result> InvalidateAllSessionsAsync(Guid userId);
    }
}
