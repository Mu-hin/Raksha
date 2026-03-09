using Raksha.Domain.Common;

namespace Raksha.Domain.Entities
{
    public class RefreshToken : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public string JwtToken { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? CreatedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsExpired && !IsRevoked;
    }
}
