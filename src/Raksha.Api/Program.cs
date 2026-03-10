

using Microsoft.EntityFrameworkCore;
using Raksha.Api.Endpoints;
using Raksha.Api.Middleware;
using Raksha.Infrastructure;
using Raksha.Infrastructure.Data;
using Raksha.Infrastructure.Data.Seeds;

namespace Raksha.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddInfrastructure(builder.Configuration);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddSingleton(TimeProvider.System); //registers Time Provider

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            var app = builder.Build();

            // Seed roles and admin user
            await SeedIdentityDataAsync(app);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseExceptionHandler();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseMiddleware<TokenBlacklistMiddleware>();
            app.UseAuthorization();

            app.MapAuthEndpoints();
            app.MapUserEndpoints();
            app.MapProfileEndpoints();
            app.MapAuditEndpoints();

            app.Run();
        }

        private static async Task SeedIdentityDataAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
            await seeder.SeedAsync();
        }
    }
}
