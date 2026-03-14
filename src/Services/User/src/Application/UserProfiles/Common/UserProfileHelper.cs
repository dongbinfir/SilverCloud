namespace User.Application.UserProfiles.Common
{
    internal class UserProfileHelper
    {
        private static readonly Random _random = new Random();
        private static readonly object _lock = new object();

        /// <summary>
        /// 生成或获取用户名。如果 name 为空或空白，则生成 "长青伴" + 随机数字
        /// </summary>
        /// <param name="name">原始用户名</param>
        /// <returns>用户名</returns>
        public static string GetOrCreateName(string? name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            // 生成唯一的随机数字
            lock (_lock)
            {
                // 使用时间戳的后6位 + 随机数来增加唯一性
                var timestamp = DateTime.UtcNow.Ticks % 1000000;
                var randomNum = _random.Next(1000, 10000);
                var uniqueNum = (timestamp + randomNum) % 10000000;

                return $"长青伴{uniqueNum}";
            }
        }
    }
}
