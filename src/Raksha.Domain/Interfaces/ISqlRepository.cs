using Raksha.Domain.Common;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Raksha.Domain.Interfaces
{
    public interface ISqlRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        #region Query

        IQueryable<TEntity> Query(bool? isActive = null, bool withDeleted = false,
            bool isAsNoTracking = true);

        #endregion

        #region Get (Single)

        Task<TEntity?> GetAsync(TKey id,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<TResult?> GetAsync<TResult>(TKey id,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<TResult?> GetAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        #endregion

        #region Load (List)

        Task<List<TEntity>> LoadAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<List<TResult>> LoadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default);

        #endregion

        #region Operations

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
        Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task DeletePermanentlyAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<int> ExecuteSqlCommandAsync(string queryText, int timeout = 60);
        Task<int> SaveChangesAsync();
        Task<int> MigrateChangesAsync();

        #endregion

        #region Raw SQL

        Task<List<TResult>> ExecuteSqlQueryAsync<TResult>(string queryText,
            IEnumerable<DbParameter> parameters, int timeout = 60,
            CommandType commandType = CommandType.Text);
        List<TResult> ExecuteSqlQueryRaw<TResult>(string queryText, IEnumerable<DbParameter> parameters);

        #endregion
    }
}
