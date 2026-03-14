using MediatR;
using Microsoft.EntityFrameworkCore;
using User.Application.Common.Interfaces;
using User.Infrastructure.Common;

namespace User.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly IMediator _mediator;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator mediator)
        : base(options)
        {
            _mediator = mediator;
        }

        // 实现接口
        public new DbSet<TEntity> Set<TEntity>() where TEntity : class
            => base.Set<TEntity>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 应用所有配置
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 注意：审计逻辑已移到 AuditableEntitySaveChangesInterceptor 拦截器中
            // 拦截器会在 base.SaveChangesAsync 执行前自动处理 Created 和 LastModified 字段

            using (var transaction = await base.Database.BeginTransactionAsync())
            {
                try
                {
                    // 先分发领域事件
                    await _mediator.DispatchDomainEvents(this);

                    // 拦截器会在下面的 base.SaveChangesAsync 执行前自动处理审计字段
                    // 这样即使领域事件处理器修改了实体，也能被正确审计
                    int result = await base.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync();

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
