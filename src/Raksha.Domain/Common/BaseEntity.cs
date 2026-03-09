namespace Raksha.Domain.Common
{
    public class BaseEntity<TKey>
    {
        public TKey Id { get; set; }
        public int Status { get; set; } = (int)EntityStatus.Active;
    }
}
