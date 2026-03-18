using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using User.Domain.Entities;

namespace User.Infrastructure.Persistence.Mongo.Configurations
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
                    // 忽略 BaseEntity 带来的 Id 属性，因为我们使用 Token 作为唯一标识
                    //cm.UnmapProperty(c => c.Id);
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        public async Task CreateIndexesAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<UserRefreshToken>("UserRefreshTokens");

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
