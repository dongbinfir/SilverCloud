using Microsoft.Extensions.Options;
using MongoDB.Driver;
using User.Application.Common.Models;
using User.Infrastructure.Common;
using User.Infrastructure.Persistence.MongoDb.Interfaces;

namespace User.Infrastructure.Persistence.MongoDb
{
    public class MongoDbContext : IMongoDbContext, IScopedDependency<IMongoDbContext>
    {
        private readonly IMongoDatabase _database;
        private readonly IEnumerable<IMongoEntityConfiguration> _configurations;

        public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> settings, IEnumerable<IMongoEntityConfiguration> configurations)
        {
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _configurations = configurations;
        }

        public IMongoCollection<T> GetCollection<T>(string? name = null)
            => _database.GetCollection<T>(name ?? MongoCollectionName.For<T>());

        public async Task InitializeAsync()
        {
            MongoMappingConfig.Register();

            foreach (var config in _configurations)
            {
                await config.CreateIndexesAsync(_database);
            }
        }
    }
}
