using MediatR;
using Microsoft.EntityFrameworkCore;
using User.Domain.Common;

namespace User.Infrastructure.Common
{
    public static class MediatorExtensions
    {
        public static async Task DispatchDomainEvents(this IMediator mediator, DbContext? context)
        {
            if (context == null) return;

            // 1. 获取所有包含领域事件的实体 (不限主键类型)
            var domainEntities = context.ChangeTracker
                .Entries<IHasDomainEvent>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            if (!domainEntities.Any()) return;

            // 2. 提取所有事件并转为列表，防止清空时丢失引用
            var domainEvents = domainEntities
                .SelectMany(x => x.DomainEvents)
                .ToList();

            // 3. 清空实体内部的事件缓存
            domainEntities.ForEach(entity => entity.ClearDomainEvents());

            // 4. 发送事件
            // 在 .NET 10 中，如果你的 Handler 很多，可以使用并发
            var tasks = domainEvents
                .Select(domainEvent => mediator.Publish(domainEvent));

            await Task.WhenAll(tasks);
        }
    }
}
