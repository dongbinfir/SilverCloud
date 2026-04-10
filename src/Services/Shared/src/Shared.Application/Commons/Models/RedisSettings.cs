namespace Shared.Application.Commons.Models
{
    /// <summary>
    /// Redis 配置模型
    /// </summary>
    public class RedisSettings
    {
        public const string SectionName = "RedisSettings";

        /// <summary>
        /// Redis 连接字符串，例如 localhost:6349
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Redis 密码
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 实例名称前缀
        /// </summary>
        public string InstanceName { get; set; } = "SilverCloud_User_";
    }
}
