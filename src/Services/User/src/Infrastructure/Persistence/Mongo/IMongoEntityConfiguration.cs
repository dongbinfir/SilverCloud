using MongoDB.Driver;

namespace User.Infrastructure.Persistence.Mongo
{
    public interface IMongoEntityConfiguration
    {
        void Configure();
        Task CreateIndexesAsync(IMongoDatabase database);
    }
}
