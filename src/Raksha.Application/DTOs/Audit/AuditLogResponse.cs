namespace Raksha.Application.DTOs.Audit
{
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
