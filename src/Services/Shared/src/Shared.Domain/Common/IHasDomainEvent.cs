namespace Shared.Domain.Common
{
    public interface IHasDomainEvent
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

        void ClearDomainEvents();
    }
}
