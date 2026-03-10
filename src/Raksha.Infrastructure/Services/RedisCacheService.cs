using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Raksha.Application.Interfaces;
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

        public async Task<string?> GetAsync(string key)
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serialized = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serialized, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        #endregion

        #region HashSet Operations

        public async Task HashSetAsync(string hashKey, string field, string value)
        {
            await _database.HashSetAsync(hashKey, field, value);
        }

        public async Task<string?> HashGetAsync(string hashKey, string field)
        {
            var value = await _database.HashGetAsync(hashKey, field);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<Dictionary<string, string>> HashGetAllAsync(string hashKey)
        {
            var entries = await _database.HashGetAllAsync(hashKey);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString());
        }

        public async Task<bool> HashRemoveAsync(string hashKey, string field)
        {
            return await _database.HashDeleteAsync(hashKey, field);
        }

        public async Task<bool> HashExistsAsync(string hashKey, string field)
        {
            return await _database.HashExistsAsync(hashKey, field);
        }

        #endregion

        #region JWT Blacklist Operations

        private const string BlacklistPrefix = "blacklist:jwt:";

        private const string BlacklistLuaScript =
            @"local ttl = tonumber(ARGV[1])
              for i = 1, #KEYS do
                  redis.call('SETEX', KEYS[i], ttl, '1')
              end
              return #KEYS";

        public async Task BlacklistJwtTokensAsync(IEnumerable<string> jwtTokens, TimeSpan ttl)
        {
            var keys = jwtTokens
                .Select(token => (RedisKey)$"{BlacklistPrefix}{HashJwt(token)}")
                .ToArray();

            if (keys.Length == 0)
                return;

            var ttlSeconds = (int)ttl.TotalSeconds;
            await _database.ScriptEvaluateAsync(BlacklistLuaScript,
                keys: keys,
                values: new RedisValue[] { ttlSeconds });
        }

        public async Task<bool> IsJwtBlacklistedAsync(string jwtToken)
        {
            var key = $"{BlacklistPrefix}{HashJwt(jwtToken)}";
            return await _database.KeyExistsAsync(key);
        }

        private static string HashJwt(string jwt)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(jwt));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion
    }
}
