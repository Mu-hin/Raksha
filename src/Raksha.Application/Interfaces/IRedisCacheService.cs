namespace Raksha.Application.Interfaces
{
    public interface IRedisCacheService
    {
        // Key-Value operations
        Task<string?> GetAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);

        // HashSet operations
        Task HashSetAsync(string hashKey, string field, string value);
        Task<string?> HashGetAsync(string hashKey, string field);
        Task<Dictionary<string, string>> HashGetAllAsync(string hashKey);
        Task<bool> HashRemoveAsync(string hashKey, string field);
        Task<bool> HashExistsAsync(string hashKey, string field);

        // JWT Blacklist operations
        Task BlacklistJwtTokensAsync(IEnumerable<string> jwtTokens, TimeSpan ttl);
        Task<bool> IsJwtBlacklistedAsync(string jwtToken);
    }
}
