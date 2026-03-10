namespace Raksha.Domain.Common
{
    public class BaseEntity<TKey>
    {
        public TKey Id { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Active;
    }
}
