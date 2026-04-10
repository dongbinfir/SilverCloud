namespace User.Application.Common.Models
{
    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// L2（Redis）过期时间，默认 10 分钟
        /// </summary>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 是否启用 L1 本地内存缓存
        /// </summary>
        public bool EnableLocalCache { get; set; } = false;

        /// <summary>
        /// L1 本地内存缓存过期时间，默认 5 分钟（仅 EnableLocalCache = true 时生效）
        /// </summary>
        public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
