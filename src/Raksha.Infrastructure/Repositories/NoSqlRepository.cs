using MongoDB.Driver;
using Raksha.Domain.Common;
using Raksha.Domain.Interfaces;
using Raksha.Infrastructure.Data;
using System.Linq.Expressions;

namespace Raksha.Infrastructure.Repositories
{
    public class NoSqlRepository<TEntity, TKey> : INoSqlRepository<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
    {
        protected readonly MongoDbContext _mongoDbContext;
        protected readonly IMongoCollection<TEntity> _collection;

        public NoSqlRepository(MongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
            _collection = mongoDbContext.Collection<TEntity>();
        }

        #region Query

        public IQueryable<TEntity> AsQueryable(bool? isActive = null, bool withDeleted = false)
        {
            var queryable = _collection.AsQueryable();

            if (isActive != null)
            {
                if (isActive.Value)
                    queryable = queryable.Where(x => x.Status == (int)EntityStatus.Active);
                else
                    queryable = queryable.Where(x => x.Status == (int)EntityStatus.Inactive);
            }

            if (!withDeleted)
                queryable = queryable.Where(x => x.Status != (int)EntityStatus.Deleted);

            return queryable;
        }

        #endregion

        #region Get (Single)

        public async Task<TEntity?> GetAsync(TKey id,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted)
                & Builders<TEntity>.Filter.Eq(e => e.Id, id);

            return await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted)
                & Builders<TEntity>.Filter.Where(predicate);

            return await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> GetAsync<TResult>(TKey id,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted)
                & Builders<TEntity>.Filter.Eq(e => e.Id, id);
            var projection = Builders<TEntity>.Projection.Expression(selector);

            return await _collection.Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> GetAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted)
                & Builders<TEntity>.Filter.Where(predicate);
            var projection = Builders<TEntity>.Projection.Expression(selector);

            return await _collection.Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted);

            if (predicate != null)
                filter &= Builders<TEntity>.Filter.Where(predicate);

            return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        public async Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted)
                & Builders<TEntity>.Filter.Where(predicate);

            return await _collection.Find(filter)
                .Limit(1)
                .AnyAsync(cancellationToken);
        }

        #endregion

        #region Load (List)

        public async Task<List<TEntity>> LoadAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted);

            if (predicate != null)
                filter &= Builders<TEntity>.Filter.Where(predicate);

            return await _collection.Find(filter)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TResult>> LoadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildBaseFilter(isActive, withDeleted);

            if (predicate != null)
                filter &= Builders<TEntity>.Filter.Where(predicate);

            var projection = Builders<TEntity>.Projection.Expression(selector);

            return await _collection.Find(filter)
                .Project(projection)
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Operations

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var entityList = entities.ToList();
            if (entityList.Count == 0) return;

            await _collection.InsertManyAsync(entityList, cancellationToken: cancellationToken);
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
            var update = Builders<TEntity>.Update
                .Set(e => e.Status, (int)EntityStatus.Deleted);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var update = Builders<TEntity>.Update
                .Set(e => e.Status, (int)EntityStatus.Deleted);

            await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }

        public async Task DeletePermanentlyAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            await _collection.DeleteOneAsync(filter, cancellationToken);
        }

        #endregion

        #region Private Helpers

        protected FilterDefinition<TEntity> BuildBaseFilter(
            bool? isActive = null, bool withDeleted = false)
        {
            var filter = Builders<TEntity>.Filter.Empty;

            if (isActive != null)
            {
                if (isActive.Value)
                    filter &= Builders<TEntity>.Filter
                        .Eq(x => x.Status, (int)EntityStatus.Active);
                else
                    filter &= Builders<TEntity>.Filter
                        .Eq(x => x.Status, (int)EntityStatus.Inactive);
            }

            if (!withDeleted)
                filter &= Builders<TEntity>.Filter
                    .Ne(x => x.Status, (int)EntityStatus.Deleted);

            return filter;
        }

        #endregion
    }
}
