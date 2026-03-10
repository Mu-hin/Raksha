using Raksha.Domain.Common;

namespace Raksha.Domain.Entities
{
    public class AuditLog : BaseEntity<string>
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
