using MongoDB.Driver;

namespace User.Infrastructure.Persistence.Mongo
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>(string? name = null);
        Task InitializeAsync();
    }
}
