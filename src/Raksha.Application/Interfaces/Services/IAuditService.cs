using Raksha.Application.DTOs.Audit;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IAuditService
    {
        Task LogAsync(Guid userId, string userEmail, string action, string details, CancellationToken ct = default);
        Task<Result<PagedResult<AuditLogResponse>>> GetPasswordChangeHistoryAsync(AuditFilterRequest filter, CancellationToken ct = default);
        Task<Result<PagedResult<AuditLogResponse>>> GetProfileUpdateHistoryAsync(AuditFilterRequest filter, CancellationToken ct = default);
    }
}
