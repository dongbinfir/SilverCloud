using Microsoft.Extensions.Caching.Hybrid;
using Shared.Application.Commons.Interfaces;
using Shared.Application.Commons.Models;

namespace Shared.Infrastructure.Caching
{
    /// <summary>
    /// 基于 HybridCache 的缓存服务实现（L1 内存 + L2 Redis）
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly HybridCache _cache;

        public HybridCacheService(HybridCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(key, factory, cancellationToken: cancellationToken);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, Action<CacheOptions> configure, CancellationToken cancellationToken = default)
        {
            var cacheOptions = new CacheOptions();
            configure(cacheOptions);

            var entryOptions = new HybridCacheEntryOptions
            {
                Expiration = cacheOptions.Expiration,
                LocalCacheExpiration = cacheOptions.LocalCacheExpiration,
                Flags = cacheOptions.EnableLocalCache
                    ? HybridCacheEntryFlags.None
                    : HybridCacheEntryFlags.DisableLocalCache
            };

            return await _cache.GetOrCreateAsync(key, factory, entryOptions, cancellationToken: cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }
}
