using Microsoft.EntityFrameworkCore;
using Raksha.Domain.Common;
using Raksha.Domain.Interfaces;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;

namespace Raksha.Infrastructure.Repositories
{
    public class SqlRepository<TEntity, TKey> : ISqlRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public async Task<List<TResult>> ExecuteSqlQueryAsync<TResult>(string queryText, IEnumerable<DbParameter> parameters, int timeout = 60, CommandType commandType = CommandType.Text)
        {
            var type = typeof(TResult);
            IList list = new List<TResult>();

            list = await ExecuteSqlQueryAsync(type, queryText, parameters: parameters, timeout, commandType);

            return (List<TResult>)list;
        }

        public List<TResult> ExecuteSqlQueryRaw<TResult>(string queryText, IEnumerable<DbParameter> parameters)
        {
            var reuslt = _dbContext.Database.SqlQueryRaw<TResult>(queryText, parameters.ToArray()).ToList();

            return reuslt;
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

                    // Add parameters to the command
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

                                    // if column doesn't contains in query result then skip that column
                                    if (resultColumns.Contains(prop.Name) == false)
                                    {
                                        continue;
                                    }

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

    }
}
