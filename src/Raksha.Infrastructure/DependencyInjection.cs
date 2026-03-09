using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Raksha.Application.Interfaces;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;
using Raksha.Infrastructure.Services;
using StackExchange.Redis;

namespace Raksha.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var postgreConnectionString = configuration.GetConnectionString("PostgreConnectionString");
            var mongoDBConnectionString = configuration.GetConnectionString("MongoDBConnectionString");
            var redisConnectionString = configuration.GetConnectionString("RedisConnectionString");

            #region postgreSQL
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(postgreConnectionString, npgsqlOptions =>
                {
                //    npgsqlOptions.MigrationsHistoryTable(DatabaseConsts.MigrationTableName,
                //DatabaseConsts.Schema);
                });
            });
            #endregion

            #region MongoDB
            services.AddSingleton<IMongoClient>(sp =>
            {
                return new MongoClient(mongoDBConnectionString);
            });

            services.AddSingleton<MongoDbContext>();
            #endregion

            #region Redis
            services.AddSingleton<IConnectionMultiplexer>(c =>
            {
                var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            #endregion

            #region Identity
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
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
            #endregion

            // Register your infrastructure services here
            // For example:
            // services.AddScoped<IMyService, MyService>();
        }
    }
}
