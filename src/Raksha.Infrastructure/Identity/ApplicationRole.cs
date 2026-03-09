using Microsoft.AspNetCore.Identity;
using Raksha.Domain.Common;

namespace Raksha.Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole<Guid>
    {   
        public ApplicationRole() : base()
        {
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
        }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public int Status { get; set; } = (int)EntityStatus.Active;
    }
}
