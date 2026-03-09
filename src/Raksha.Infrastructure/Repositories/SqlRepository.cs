using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Raksha.Domain.Common;
using Raksha.Domain.Interfaces;
using Raksha.Infrastructure.Data;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;

namespace Raksha.Infrastructure.Repositories
{
    public class SqlRepository<TEntity, TKey> : ISqlRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        protected readonly IApplicationDbContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;

        public SqlRepository(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.DbSet<TEntity>();
        }

        #region Query

        public IQueryable<TEntity> Query(bool? isActive = null, bool withDeleted = false,
            bool isAsNoTracking = true)
        {
            IQueryable<TEntity> query = _dbSet;

            if (isActive != null)
            {
                if (isActive.Value)
                    query = query.Where(x => x.Status == (int)EntityStatus.Active);
                else
                    query = query.Where(x => x.Status == (int)EntityStatus.Inactive);
            }

            if (isAsNoTracking)
                query = query.AsNoTracking();

            if (!withDeleted)
                query = query.Where(x => x.Status != (int)EntityStatus.Deleted);

            return query;
        }

        #endregion

        #region Get (Single)

        public async Task<TEntity?> GetAsync(TKey id,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);
            return await query.FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);
        }

        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);
            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<TResult?> GetAsync<TResult>(TKey id,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);
            return await query
                .Where(e => e.Id!.Equals(id))
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> GetAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);
            return await query
                .Where(predicate)
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);

            if (predicate != null)
                query = query.Where(predicate);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> predicate,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);
            return await query.AnyAsync(predicate, cancellationToken);
        }

        #endregion

        #region Load (List)

        public async Task<List<TEntity>> LoadAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);

            if (predicate is not null)
                query = query.Where(predicate);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<TResult>> LoadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? predicate = null,
            bool? isActive = null, bool withDeleted = false, bool isAsNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var query = Query(isActive, withDeleted, isAsNoTracking);

            if (predicate is not null)
                query = query.Where(predicate);

            return await query.Select(selector).ToListAsync(cancellationToken);
        }

        #endregion

        #region Operations

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet
                .FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);

            if (entity is null) return;

            entity.Status = (int)EntityStatus.Deleted;
        }

        public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var entities = await _dbSet
                .Where(predicate)
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                entity.Status = (int)EntityStatus.Deleted;
            }
        }

        public Task DeletePermanentlyAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<int> ExecuteSqlCommandAsync(string queryText, int timeout = 60)
        {
            _dbContext.Database.SetCommandTimeout(timeout);
            return await _dbContext.Database.ExecuteSqlRawAsync(queryText);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<int> MigrateChangesAsync()
        {
            return await _dbContext.MigrateChangesAsync();
        }

        #endregion

        #region Raw SQL

        public async Task<List<TResult>> ExecuteSqlQueryAsync<TResult>(string queryText, IEnumerable<DbParameter> parameters, int timeout = 60, CommandType commandType = CommandType.Text)
        {
            var type = typeof(TResult);
            IList list = new List<TResult>();

            list = await ExecuteSqlQueryAsync(type, queryText, parameters: parameters, timeout, commandType);

            return (List<TResult>)list;
        }

        public List<TResult> ExecuteSqlQueryRaw<TResult>(string queryText, IEnumerable<DbParameter> parameters)
        {
            var result = _dbContext.Database.SqlQueryRaw<TResult>(queryText, parameters.ToArray()).ToList();
            return result;
        }

        #region Helper

        private async Task<IList> ExecuteSqlQueryAsync(Type entityType, string queryText, IEnumerable<DbParameter>? parameters = null,
            int timeout = 60, CommandType commandType = CommandType.Text)
        {
            bool isNewConnection = false;
            bool useCurrentTransaction = true;

            Type genericListType = typeof(List<>).MakeGenericType(entityType);
            IList? list = (IList)Activator.CreateInstance(genericListType)!;

            var conn = _dbContext.Database.CurrentTransaction?
                .GetDbTransaction().Connection;
            if (conn == null)
            {
                useCurrentTransaction = false;
                conn = _dbContext.Database.GetDbConnection();
            }

            try
            {
                var entityProperties = entityType.GetProperties();
                if (conn.State == ConnectionState.Closed)
                {
                    conn.OpenAsync().Wait();
                    isNewConnection = true;
                }

                using (var command = conn.CreateCommand())
                {
                    if (useCurrentTransaction)
                    {
                        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
                    }

                    command.CommandTimeout = timeout;
                    command.CommandType = commandType;
                    command.CommandText = queryText;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                    }

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        List<string> resultColumns = new List<string>();

                        var schemaTable = await reader.GetSchemaTableAsync();
                        var columnName = schemaTable?.Columns["ColumnName"];

                        foreach (DataRow tableField in schemaTable!.Rows)
                        {
                            resultColumns.Add(tableField[columnName!].ToString()!);
                        }

                        while (reader.Read())
                        {
                            try
                            {
                                var objArr = new object[reader.FieldCount];
                                var value = reader.GetValues(objArr);
                                var obj = Activator.CreateInstance(entityType);

                                for (int i = 0; i < entityProperties.Length; i++)
                                {
                                    var prop = entityProperties[i];

                                    if (resultColumns.Contains(prop.Name) == false)
                                        continue;

                                    var ordinal = reader.GetOrdinal(prop.Name);
                                    if (ordinal >= 0 && reader.IsDBNull(ordinal) == false)
                                    {
                                        if (prop.PropertyType == typeof(Int16) || prop.PropertyType == typeof(Int16?))
                                        {
                                            var val = reader.GetInt16(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                                        {
                                            var val = reader.GetInt32(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                                        {
                                            var val = reader.GetInt64(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                                        {
                                            var val = reader.GetGuid(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(string))
                                        {
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            var val = reader.GetString(ordinal);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(float)
                                            || prop.PropertyType == typeof(float?))
                                        {
                                            var val = reader.GetFloat(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(double)
                                            || prop.PropertyType == typeof(double?))
                                        {
                                            var val = reader.GetDouble(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(decimal)
                                            || prop.PropertyType == typeof(decimal?))
                                        {
                                            var val = reader.GetDecimal(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(bool)
                                            || prop.PropertyType == typeof(bool?))
                                        {
                                            var val = reader.GetBoolean(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(DateTime)
                                            || prop.PropertyType == typeof(DateTime?))
                                        {
                                            var val = reader.GetDateTime(ordinal);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, val);
                                        }
                                        else if (prop.PropertyType == typeof(TimeOnly) || prop.PropertyType == typeof(TimeOnly?))
                                        {
                                            var val = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
                                            if (!string.IsNullOrEmpty(val) && TimeOnly.TryParseExact(val, "H:mm", null, DateTimeStyles.None, out var timeOnlyValue))
                                            {
                                                var propInfo = entityType.GetProperty(prop.Name);
                                                propInfo?.SetValue(obj, timeOnlyValue);
                                            }
                                        }
                                        else if (prop.PropertyType == typeof(DateOnly)
                                            || prop.PropertyType == typeof(DateOnly?))
                                        {
                                            var val = reader.GetDateTime(ordinal);
                                            var dateOnly = DateOnly.FromDateTime(val);
                                            var propInfo = entityType.GetProperty(prop.Name);
                                            propInfo?.SetValue(obj, dateOnly);
                                        }
                                    }
                                }

                                list.Add(obj);
                            }
                            catch (Exception)
                            {
                                reader.Close();
                                throw;
                            }
                        }
                        reader.Close();
                    }
                }

                if (isNewConnection && conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
            catch (Exception)
            {
                if (isNewConnection && conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }

                throw;
            }

            return list;
        }

        #endregion

        #endregion
    }
}
