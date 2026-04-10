using Identity.Domain.MongoDbEntities;

namespace Identity.Application.Commons.MongoDbRepositories
{
    public interface IAccountRefreshTokenRepository : IMongoDbRepository<AccountRefreshToken>
    {
        Task<AccountRefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        Task<List<AccountRefreshToken>> GetListByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

        Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default);

        Task DeleteByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    }
}
