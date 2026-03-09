namespace Raksha.Domain.Common
{
    public class BaseAuditableEntity<TKey> : BaseEntity<TKey>
    {
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
    }
}
