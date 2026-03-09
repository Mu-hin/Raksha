using Microsoft.AspNetCore.Identity;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;

namespace Raksha.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public UserDetails UserDetails { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public int Status { get; set; } = (int)EntityStatus.Active;
    }
}
