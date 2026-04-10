using MongoDB.Driver;
using Shared.Application.Commons.Interfaces;
using Shared.Domain.Common;
using Shared.Infrastructure.Persistence.MongoDb.Interfaces;
using System.Linq.Expressions;

namespace Shared.Infrastructure.Persistence.MongoDb.Repositories
{
    public class MongoDbRepository<T> : IMongoDbRepository<T> where T : MongoEntity
    {
        protected readonly IMongoCollection<T> _collection;
        protected readonly ICurrentAccountService _currentAccountService;

        public MongoDbRepository(IMongoDbContext mongoDbContext,
            ICurrentAccountService currentAccountService)
        {
            _collection = mongoDbContext.GetCollection<T>();
            _currentAccountService = currentAccountService;
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // 假设实体中包含 Id 属性。MongoDB 驱动会自动处理 Guid 到 _id 的映射
            var filter = Builders<T>.Filter.Eq(a => a.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.Created = DateTime.UtcNow;
            entity.CreatedBy = _currentAccountService.Id;

            await _collection.InsertOneAsync(entity, null, cancellationToken);
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.Eq(a => a.Id, entity.Id);

            entity.LastModified = DateTime.UtcNow;
            entity.LastModifiedBy = _currentAccountService.Id;

            // 使用 Replace 模式更新整个实体
            await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }

        public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.Eq(a => a.Id, id);
            await _collection.DeleteOneAsync(filter, cancellationToken);
        }
    }
}
