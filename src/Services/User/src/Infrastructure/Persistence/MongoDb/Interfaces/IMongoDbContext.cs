using MongoDB.Driver;

namespace User.Infrastructure.Persistence.MongoDb.Interfaces
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>(string? name = null);
        Task InitializeAsync();
    }
}
