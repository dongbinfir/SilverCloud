namespace User.Application.Common.Interfaces
{
    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或设置缓存（使用默认配置）
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取或设置缓存（自定义配置：过期时间、是否启用 L1、L1 过期时间）
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, Action<CacheOptions> configure, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除缓存
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
