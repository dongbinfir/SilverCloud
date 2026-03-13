using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using User.Domain.Common;

namespace User.Domain.ValueObjects
{
    public class Email : ValueObject
    {
        // 使用预编译的 Regex 提高性能
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        // 私有构造函数，强制通过 Create 方法创建，确保校验逻辑不被跳过
        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email 不能为空");
            }

            string trimmedEmail = email.Trim();

            if (!EmailRegex.IsMatch(trimmedEmail))
            {
                throw new ArgumentException("Email 格式不正确");
            }

            return new Email(trimmedEmail);
        }

        // 核心：告诉基类哪些属性参与相等性比较
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value.ToLowerInvariant(); // Email 通常不区分大小写
        }

        // 隐式转换：让你在使用 string 的地方可以直接用 Email 对象（可选）
        public static implicit operator string(Email email) => email.Value;

        public override string ToString() => Value;
    }
}
