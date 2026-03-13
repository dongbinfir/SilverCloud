using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using User.Domain.Entities;
using User.Domain.ValueObjects;

namespace User.Infrastructure.Data.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // 表名
            builder.ToTable("UserProfiles");

            builder.Property(u => u.Name)
                    .HasMaxLength(50)
                    .IsRequired();

            // 主键
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Email)
                .HasConversion(
                    v => v != null ? v.Value : null,          // 存入：如果对象不为空则取其值，否则存 null
                    v => v != null ? Email.Create(v) : null!)    // 读取：如果数据库值不为空则创建对象，否则返回 null
                .HasMaxLength(150);

            // 可选项，普通字符串
            builder.Property(u => u.PhoneNum)
                .HasMaxLength(20);

            // 必填项，通常哈希值长度固定
            builder.Property(u => u.Password)
                .HasMaxLength(256)
                .IsRequired();
        }
    }
}
