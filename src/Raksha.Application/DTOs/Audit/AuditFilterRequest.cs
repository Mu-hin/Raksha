namespace Raksha.Application.DTOs.Audit
{
    public class AuditFilterRequest
    {
        public Guid? UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
