using Raksha.Domain.Common;

namespace Raksha.Domain.Entities
{
    public class UserDetails : BaseAuditableEntity<Guid>
    {
        public Guid UserId { get; set; }
        public string FullName {  get; set; } = string.Empty;
        public string LastName {  get; set; } = string.Empty;
        public string? ImageKey { get; set; }
    }
}
