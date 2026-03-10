using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasOne(u => u.UserDetails)
                .WithOne()
                .HasForeignKey<UserDetails>(ud => ud.UserId);

            builder.HasMany<RefreshToken>(m => m.RefreshTokens)
                .WithOne()
                .HasForeignKey(ud => ud.UserId);
        }
    }
}
