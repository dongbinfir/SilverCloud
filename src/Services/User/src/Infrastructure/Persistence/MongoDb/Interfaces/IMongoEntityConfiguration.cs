using MongoDB.Driver;

namespace User.Infrastructure.Persistence.MongoDb.Interfaces
{
    public interface IMongoEntityConfiguration
    {
        Task CreateIndexesAsync(IMongoDatabase database);
    }
}
