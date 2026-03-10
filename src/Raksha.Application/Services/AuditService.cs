using Microsoft.Extensions.Logging;
using Raksha.Application.DTOs.Audit;
using Raksha.Application.Interfaces.Repositories;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Entities;

namespace Raksha.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IAuditRepository auditRepository,
            ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        public async Task LogAsync(Guid userId, string userEmail, string action, string details, CancellationToken ct = default)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(auditLog, ct);
            _logger.LogInformation("Audit log created: {Action} for user {UserId}", action, userId);
        }

        public async Task<Result<PagedResult<AuditLogResponse>>> GetPasswordChangeHistoryAsync(AuditFilterRequest filter, CancellationToken ct = default)
        {
            return await GetAuditLogsAsync("PasswordChange", filter);
        }

        public async Task<Result<PagedResult<AuditLogResponse>>> GetProfileUpdateHistoryAsync(AuditFilterRequest filter, CancellationToken ct = default)
        {
            return await GetAuditLogsAsync("ProfileUpdate", filter);
        }

        private async Task<Result<PagedResult<AuditLogResponse>>> GetAuditLogsAsync(string action, AuditFilterRequest filter)
        {
            var query = _auditRepository.AsQueryable()
                .Where(a => a.Action == action);

            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(a => a.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    Action = a.Action,
                    Details = a.Details,
                    Timestamp = a.Timestamp
                })
                .ToList();

            var pagedResult = new PagedResult<AuditLogResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return await Task.FromResult(Result<PagedResult<AuditLogResponse>>.Success(data: pagedResult));
        }
    }
}
