using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Raksha.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var postgreConnectionString = configuration.GetConnectionString("PostgreConnectionString");
            var mongoDBConnectionString = configuration.GetConnectionString("MongoDBConnectionString");
            var redisConnectionString = configuration.GetConnectionString("redisConnectionString");
            
            
            // Register your infrastructure services here
            // For example:
            // services.AddScoped<IMyService, MyService>();
        }
    }
}
