using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Raksha.Infrastructure.Data
{
    public class MongoDbContext
    {
        internal readonly IMongoDatabase _database;

        public MongoDbContext(IMongoClient client, IConfiguration configuration)
        {
            var mongoUrl = MongoUrl.Create(configuration.GetConnectionString("MongoDBConnectionString"));
            _database = client.GetDatabase(mongoUrl.DatabaseName);
        }
    }
}
