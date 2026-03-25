using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using User.Domain.MongoDbEntities;
using User.Infrastructure.Common;
using User.Infrastructure.Persistence.MongoDb.Interfaces;

namespace User.Infrastructure.Persistence.MongoDb.Configurations
{
    public class RefreshTokenConfiguration : IMongoEntityConfiguration
    {
        public void Configure()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(UserRefreshToken)))
            {
                BsonClassMap.RegisterClassMap<UserRefreshToken>(cm =>
                {
                    cm.AutoMap();
                    // 将 Token 作为 MongoDB 的 _id
                    cm.MapIdProperty(c => c.Id);
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        public async Task CreateIndexesAsync(IMongoDatabase database)
        {
            var collectionName = MongoCollectionName.For<UserRefreshToken>();
            var collection = database.GetCollection<UserRefreshToken>(collectionName);

            // 1. 创建 TTL 索引：当当前时间超过 ExpiresAt 时，MongoDB 自动删除该文档
            var ttlIndexKeys = Builders<UserRefreshToken>.IndexKeys.Ascending(x => x.ExpiresAt);
            var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserRefreshToken>(ttlIndexKeys, ttlOptions));

            // 2. 创建 UserProfileId 索引：优化用户 Token 查询性能
            var userIndexKeys = Builders<UserRefreshToken>.IndexKeys.Ascending(x => x.UserProfileId);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserRefreshToken>(userIndexKeys));
        }
    }
}
