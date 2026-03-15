using System;
using System.Collections.Generic;
using System.Text;

namespace User.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        /// <summary>
        /// 对密码进行哈希处理
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// 验证密码是否匹配哈希值
        /// </summary>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
