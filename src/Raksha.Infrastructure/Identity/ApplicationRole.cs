using Microsoft.AspNetCore.Identity;

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
    }
}
