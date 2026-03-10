using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Raksha.Infrastructure.Data.Seeds;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure.Data
{
    public class RoleSeeder : IRoleSeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<ApplicationRole> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedRolesAsync()
        {
            var roleSeed = new ApplicationRoleSeed();
            
            foreach (var role in roleSeed.ApplicationRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role.Name!))
                {
                    var result = await _roleManager.CreateAsync(role);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role '{RoleName}' created successfully.", role.Name);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogWarning("Failed to create role '{RoleName}': {Errors}", role.Name, errors);
                    }
                }
                else
                {
                    _logger.LogInformation("Role '{RoleName}' already exists.", role.Name);
                }
            }
        }
    }
}
