using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Raksha.Application.Interfaces;
using Raksha.Application.Models;
using Raksha.Domain.Interfaces;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Identity;
using Raksha.Infrastructure.Repositories;
using Raksha.Infrastructure.Services;
using StackExchange.Redis;
using System.Text;

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
                var config = ConfigurationOptions.Parse(redisConnectionString!, true);
                return ConnectionMultiplexer.Connect(config);
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

            #region DbContext & Repository
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped(typeof(ISqlRepository<,>), typeof(SqlRepository<,>));
            services.AddScoped(typeof(INoSqlRepository<,>), typeof(NoSqlRepository<,>));
            #endregion

            #region JWT Authentication
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    //ClockSkew = TimeSpan.Zero
                };

                //options.MapInboundClaims = true;
            });
            #endregion

            #region Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IIdentitySeeder, IdentitySeeder>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IFileService, FileService>();
            services.Configure<FileSettings>(configuration.GetSection(FileSettings.SectionName));
            #endregion
        }
    }
}
