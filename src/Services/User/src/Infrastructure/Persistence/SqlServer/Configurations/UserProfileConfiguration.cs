using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace User.Infrastructure.Persistence.SqlServer.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // 表名
            builder.ToTable(MongoCollectionName.For<UserProfile>());

            // 主键
            builder.HasKey(p => p.Id);
        }
    }
}
