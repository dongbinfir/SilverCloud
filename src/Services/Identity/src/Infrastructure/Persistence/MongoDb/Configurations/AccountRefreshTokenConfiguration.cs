using Identity.Domain.MongoDbEntities;

namespace Identity.Infrastructure.Persistence.MongoDb.Configurations
{
    public class AccountRefreshTokenConfiguration : IMongoEntityConfiguration
    {
        public async Task CreateIndexesAsync(IMongoDatabase database)
        {
            var collectionName = MongoCollectionName.For<AccountRefreshToken>();
            var collection = database.GetCollection<AccountRefreshToken>(collectionName);

            // 1. 创建 TTL 索引：当当前时间超过 ExpiresAt 时，MongoDB 自动删除该文档
            var ttlIndexKeys = Builders<AccountRefreshToken>.IndexKeys.Ascending(x => x.ExpiresAt);
            var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<AccountRefreshToken>(ttlIndexKeys, ttlOptions));

            // 2. 创建 UserProfileId 索引：优化用户 Token 查询性能
            var userIndexKeys = Builders<AccountRefreshToken>.IndexKeys.Ascending(x => x.AccountInfoId);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<AccountRefreshToken>(userIndexKeys));
        }
    }
}
