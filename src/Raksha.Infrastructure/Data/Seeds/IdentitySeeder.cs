using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Data.Seeds
{
    public class IdentitySeeder : IIdentitySeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IdentitySeeder> _logger;

        public IdentitySeeder(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<IdentitySeeder> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedAdminUserAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roleSeed = new ApplicationRoleSeed();

            foreach (var role in roleSeed.ApplicationRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role.Name!))
                {
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                        _logger.LogInformation("Role '{RoleName}' created successfully.", role.Name);
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogWarning("Failed to create role '{RoleName}': {Errors}", role.Name, errors);
                    }
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            const string adminEmail = "admin@raksha.com";
            const string adminPassword = "Admin12#";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
                return;

            var adminUser = new ApplicationUser
            {
                Email = adminEmail,
                UserName = "admin",
                EmailConfirmed = true,
                Status = (int)EntityStatus.Active,
                UserDetails = new UserDetails
                {
                    FirstName = "System",
                    LastName = "Administrator"
                },
                RefreshTokens = new List<RefreshToken>()
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to seed admin user: {Errors}", errors);
                return;
            }

            var roleResult = await _userManager.AddToRoleAsync(adminUser, Roles.Admin);

            if (roleResult.Succeeded)
                _logger.LogInformation("Admin user seeded successfully with email '{Email}'.", adminEmail);
            else
                _logger.LogWarning("Admin user created but failed to assign Admin role.");
        }
    }
}
