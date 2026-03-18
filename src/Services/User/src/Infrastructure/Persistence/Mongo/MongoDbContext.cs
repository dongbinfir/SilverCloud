using Microsoft.Extensions.Options;
using MongoDB.Driver;
using User.Application.Common.Models;

namespace User.Infrastructure.Persistence.Mongo
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IEnumerable<IMongoEntityConfiguration> _configurations;

        public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> settings, IEnumerable<IMongoEntityConfiguration> configurations)
        {
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _configurations = configurations;
        }

        public IMongoCollection<T> GetCollection<T>(string? name = null)
            => _database.GetCollection<T>(name ?? typeof(T).Name + "s");

        public async Task InitializeAsync()
        {
            foreach (var config in _configurations)
            {
                config.Configure();
                await config.CreateIndexesAsync(_database);
            }
        }
    }
}
