using MongoDB.Driver;

namespace User.Infrastructure.Persistence.MongoDb.Interfaces
{
    public interface IMongoEntityConfiguration
    {
        void Configure();
        Task CreateIndexesAsync(IMongoDatabase database);
    }
}
