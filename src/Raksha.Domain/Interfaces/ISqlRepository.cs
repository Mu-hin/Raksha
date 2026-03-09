
using Raksha.Domain.Common;
using System.Linq.Expressions;

namespace Raksha.Domain.Interfaces
{
    public interface ISqlRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        public Task<TKey> PermanentDelete(Guid id, CancellationToken cancellationToken = default);
        public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        public Task<TResult?> GetAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default);
        public Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken = default);
        public Task<TResult?> GetAsync<TResult>(TKey id, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default);
        Task<List<TResult>> LoadAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default);
    }
}
