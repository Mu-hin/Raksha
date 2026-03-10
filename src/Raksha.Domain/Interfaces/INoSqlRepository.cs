using Raksha.Domain.Common;
using System.Linq.Expressions;

namespace Raksha.Domain.Interfaces
{
    public interface INoSqlRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        #region Query

        IQueryable<TEntity> AsQueryable(bool? isActive = null, bool withDeleted = false);

        #endregion

        #region Get (Single)

        Task<TEntity?> GetAsync(TKey id,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<TResult?> GetAsync<TResult>(TKey id,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<TResult?> GetAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        #endregion

        #region Load (List)

        Task<List<TEntity>> LoadAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        Task<List<TResult>> LoadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default);

        #endregion

        #region Operations

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
        Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task DeletePermanentlyAsync(TEntity entity, CancellationToken cancellationToken = default);

        #endregion
    }
}
