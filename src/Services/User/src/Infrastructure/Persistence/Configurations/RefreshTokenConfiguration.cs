using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using User.Domain.Entities;

namespace User.Infrastructure.Data.Configurations
{
    public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            // 表名
            builder.ToTable("UserRefreshTokens");

            // 主键
            builder.HasKey(p => p.Id);

            builder.Property(u => u.Token)
                .HasMaxLength(512);

            builder.Property(u => u.CreatedByIp)
                .HasMaxLength(64);

            builder.Property(u => u.RevokedByIp)
                .HasMaxLength(64);

            builder.Property(u => u.RevokedReason)
                .HasMaxLength(256);

            // 索引
            builder.HasIndex(rt => rt.UserProfileId);
            builder.HasIndex(rt => rt.Token);
            builder.HasIndex(rt => rt.ExpiresAt);
        }
    }
}
