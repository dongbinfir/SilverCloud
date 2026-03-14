namespace User.Domain.Common
{
    public abstract class BaseEntity<T> : IHasDomainEvent
    {
        public T Id { get; set; } = default!;

        // 内部私有列表，用于实际操作
        private readonly List<IDomainEvent> _domainEvents = new();
        
        // 显式实现接口或正常暴露只读版本
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
