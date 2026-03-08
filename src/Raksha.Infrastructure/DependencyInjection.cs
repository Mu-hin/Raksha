using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;

namespace Raksha.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var postgreConnectionString = configuration.GetConnectionString("PostgreConnectionString");
            var mongoDBConnectionString = configuration.GetConnectionString("MongoDBConnectionString");
            var redisConnectionString = configuration.GetConnectionString("redisConnectionString");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(postgreConnectionString, npgsqlOptions =>
                {
                //    npgsqlOptions.MigrationsHistoryTable(DatabaseConsts.MigrationTableName,
                //DatabaseConsts.Schema);
                });
            });

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            // Register your infrastructure services here
            // For example:
            // services.AddScoped<IMyService, MyService>();
        }
    }
}
