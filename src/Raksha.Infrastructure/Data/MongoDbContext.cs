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

        public IMongoCollection<TEntity> Collection<TEntity>() where TEntity : class
        {
            return _database.GetCollection<TEntity>(GetCollectionName<TEntity>());
        }

        private static string GetCollectionName<TEntity>()
        {
            return typeof(TEntity).Name + "s";
        }
    }
}
