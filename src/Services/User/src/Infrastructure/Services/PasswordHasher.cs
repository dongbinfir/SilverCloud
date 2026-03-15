using User.Application.Common.Interfaces;

namespace User.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // BCrypt 自动生成盐值并哈希
            // 工作因子默认为 11，可调整
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            // 验证明文密码是否与哈希值匹配
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
