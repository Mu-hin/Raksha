using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Data
{
    internal class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
    }
}
