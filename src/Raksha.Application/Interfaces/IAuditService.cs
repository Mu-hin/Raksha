using Raksha.Application.DTOs.Audit;
using Raksha.Application.Models;

namespace Raksha.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(Guid userId, string userEmail, string action, string details);
        Task<Result<PagedResult<AuditLogResponse>>> GetPasswordChangeHistoryAsync(AuditFilterRequest filter);
        Task<Result<PagedResult<AuditLogResponse>>> GetProfileUpdateHistoryAsync(AuditFilterRequest filter);
    }
}
