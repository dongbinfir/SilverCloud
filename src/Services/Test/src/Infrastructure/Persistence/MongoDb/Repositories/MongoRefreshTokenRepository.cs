using MongoDB.Driver;
using User.Application.Common.Interfaces;
using User.Domain.MongoDbEntities;
using User.Infrastructure.Common;
using User.Infrastructure.Persistence.MongoDb.Interfaces;

namespace User.Infrastructure.Persistence.MongoDb.Repositories
{
    public class MongoRefreshTokenRepository : IRefreshTokenRepository, IScopedDependency<IRefreshTokenRepository>
    {
        private readonly IMongoCollection<UserRefreshToken> _collection;
        private readonly ICurrentUserService _currentUserService;

        public MongoRefreshTokenRepository(IMongoDbContext dbContext, ICurrentUserService currentUserService)
        {
            _collection = dbContext.GetCollection<UserRefreshToken>();
            _currentUserService = currentUserService;
        }

        public async Task<UserRefreshToken?> GetByTokenAsync(string token)
        {
            return await _collection.Find(x => x.Token == token).FirstOrDefaultAsync();
        }

        public async Task<List<UserRefreshToken>> GetActiveTokensByUserIdAsync(int userId)
        {
            // 在 MongoDB 中，IsActive 对应的逻辑是 RevokedAt == null && ExpiresAt > DateTime.UtcNow
            return await _collection.Find(rt =>
                rt.UserProfileId == userId &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task AddAsync(UserRefreshToken refreshToken)
        {
            refreshToken.Created = DateTime.Now;
            refreshToken.CreatedBy = _currentUserService.UserId;

            await _collection.InsertOneAsync(refreshToken);
        }

        public async Task UpdateAsync(UserRefreshToken refreshToken)
        {
            refreshToken.LastModified = DateTime.Now;
            refreshToken.LastModifiedBy = _currentUserService.UserId;

            await _collection.ReplaceOneAsync(x => x.Token == refreshToken.Token, refreshToken);
        }

        public async Task DeleteAsync(string token)
        {
            await _collection.DeleteOneAsync(x => x.Token == token);
        }

        public async Task DeleteAllByUserIdAsync(int userId)
        {
            await _collection.DeleteManyAsync(x => x.UserProfileId == userId);
        }
    }
}
