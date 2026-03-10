using System.Text.Json;
using Raksha.Application.Interfaces.Services;
using StackExchange.Redis;

namespace Raksha.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
        {
            _database = connectionMultiplexer.GetDatabase();
        }

        #region Key-Value Operations

        public async Task<string?> GetAsync(string key, CancellationToken ct = default)
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            var serialized = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serialized, expiry);
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return await _database.KeyExistsAsync(key);
        }

        #endregion

        #region HashSet Operations

        public async Task HashSetAsync(string hashKey, string field, string value, CancellationToken ct = default)
        {
            await _database.HashSetAsync(hashKey, field, value);
        }

        public async Task<string?> HashGetAsync(string hashKey, string field, CancellationToken ct = default)
        {
            var value = await _database.HashGetAsync(hashKey, field);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<Dictionary<string, string>> HashGetAllAsync(string hashKey, CancellationToken ct = default)
        {
            var entries = await _database.HashGetAllAsync(hashKey);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString());
        }

        public async Task<bool> HashRemoveAsync(string hashKey, string field, CancellationToken ct = default)
        {
            return await _database.HashDeleteAsync(hashKey, field);
        }

        public async Task<bool> HashExistsAsync(string hashKey, string field, CancellationToken ct = default)
        {
            return await _database.HashExistsAsync(hashKey, field);
        }

        #endregion

        #region Lua Script Execution

        public async Task<long> ExecuteLuaScriptAsync(string script, string[] keys, string[] values, CancellationToken ct = default)
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            var result = await _database.ScriptEvaluateAsync(script, redisKeys, redisValues);
            return (long)result;
        }

        #endregion
    }
}
