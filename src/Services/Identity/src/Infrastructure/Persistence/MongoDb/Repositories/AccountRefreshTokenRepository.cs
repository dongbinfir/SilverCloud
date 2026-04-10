using Identity.Application.Commons.MongoDbRepositories;
using Identity.Domain.MongoDbEntities;
using Shared.Infrastructure.Persistence.MongoDb.Repositories;

namespace Identity.Infrastructure.Persistence.MongoDb.Repositories
{
    public class AccountRefreshTokenRepository : MongoDbRepository<AccountRefreshToken>, IAccountRefreshTokenRepository, IScopedDependency<IAccountRefreshTokenRepository>
    {
        public AccountRefreshTokenRepository(IMongoDbContext mongoDbContext,
        ICurrentAccountService currentAccountService)
        : base(mongoDbContext, currentAccountService)
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
