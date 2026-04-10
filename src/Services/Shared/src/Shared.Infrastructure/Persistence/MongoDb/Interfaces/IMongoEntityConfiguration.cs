using MongoDB.Driver;

namespace Shared.Infrastructure.Persistence.MongoDb.Interfaces
{
    public interface IMongoEntityConfiguration
    {
        Task CreateIndexesAsync(IMongoDatabase database);
    }
}
