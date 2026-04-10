using System.Threading.RateLimiting;

namespace Identity.WebAPI.Helpers
{
    /// <summary>
    /// 速率限制辅助类
    /// </summary>
    public static class RateLimiterHelper
    {
        /// <summary>
        /// 创建固定窗口限流器配置
        /// </summary>
        /// <param name="permitLimit">允许的请求数量</param>
        /// <param name="windowMinutes">时间窗口（分钟）</param>
        /// <param name="queueLimit">队列限制，默认为 0（直接拒绝）</param>
        /// <returns>固定窗口限流器配置</returns>
        public static FixedWindowRateLimiterOptions CreateFixedWindowLimiter(
            int permitLimit,
            int windowMinutes,
            int queueLimit = 0)
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = queueLimit,
                AutoReplenishment = true
            };
        }

        /// <summary>
        /// 创建滑动窗口限流器配置
        /// </summary>
        /// <param name="permitLimit">允许的请求数量</param>
        /// <param name="windowMinutes">时间窗口（分钟）</param>
        /// <param name="segmentsPerWindow">窗口分段数，默认为 2</param>
        /// <param name="queueLimit">队列限制，默认为 0（直接拒绝）</param>
        /// <returns>滑动窗口限流器配置</returns>
        public static SlidingWindowRateLimiterOptions CreateSlidingWindowLimiter(
            int permitLimit,
            int windowMinutes,
            int segmentsPerWindow = 2,
            int queueLimit = 0)
        {
            return new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                SegmentsPerWindow = segmentsPerWindow,
                QueueLimit = queueLimit
            };
        }
    }
}
