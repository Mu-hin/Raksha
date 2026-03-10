using Raksha.Domain.Common;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Data.Seeds
{
    internal class ApplicationRoleSeed
    {
        internal List<ApplicationRole> ApplicationRoles
        {
            get
            {
                return new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = Guid.Parse("95E139BB-6751-4D4B-B14F-12E1597EF982"),
                        Name = "Admin",
                        NormalizedName = "ADMIN",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System",
                        LastModifiedAt = DateTime.UtcNow,
                        LastModifiedBy = "System",
                        Status = EntityStatus.Active
                    },
                    new ApplicationRole
                    {
                        Id = Guid.Parse("1FBD35A5-EC5A-48CE-9D93-47CA2494FF11"),
                        Name = "User",
                        NormalizedName = "USER",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System",
                        LastModifiedAt = DateTime.UtcNow,
                        LastModifiedBy = "System",
                        Status = EntityStatus.Active
                    }
                };
            }
        }
    }
}
