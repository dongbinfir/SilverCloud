using Identity.Application.Commons.MongoDbRepositories;
using Identity.Domain.MongoDbEntities;

namespace Identity.Infrastructure.Persistence.MongoDb.Repositories
{
    public class AccountRefreshTokenRepository : MongoDbRepository<AccountRefreshToken>, IAccountRefreshTokenRepository, IScopedDependency<IAccountRefreshTokenRepository>
    {
        public AccountRefreshTokenRepository(IMongoCollection<AccountRefreshToken> _collection,
        ICurrentAccountService _currentAccountService)
        : base(_collection, _currentAccountService)
        {
        }

        public Task DeleteByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AccountRefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<AccountRefreshToken>> GetListByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
