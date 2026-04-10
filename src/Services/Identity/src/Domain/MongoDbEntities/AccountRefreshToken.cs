namespace Identity.Domain.MongoDbEntities
{
    public class AccountRefreshToken : MongoEntity
    {
        public AccountRefreshToken()
        {
            Id = Guid.CreateVersion7();
        }

        public int AccountInfoId { get; set; }

        // 实际的 Token 字符串（建议使用加密随机数，不要存明文 JWT）
        public string Token { get; set; } = string.Empty;

        // 到期时间
        public DateTime ExpiresAt { get; set; }

        // 创建该 Token 的 IP 地址（安全审计用）
        public string CreatedByIp { get; set; } = string.Empty;

        // 撤销时间（如果用户手动注销或后台拉黑）
        public DateTime? RevokedAt { get; set; }

        // 撤销该 Token 的 IP 地址
        public string? RevokedByIp { get; set; }

        // 撤销原因（如：用户注销、检测到异常更换等）
        public string? RevokedReason { get; set; }

        // 替换关系（滚动更新）
        public Guid? PreviousRefreshTokenId { get; set; }
        public Guid? NextRefreshTokenId { get; set; }

        // 辅助属性：判断是否已过期
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        // 辅助属性：判断是否已失效（已撤销或已过期）
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}
