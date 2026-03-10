namespace Raksha.Application.Interfaces.Services
{
    public interface IRedisCacheService
    {
        // Key-Value operations
        Task<string?> GetAsync(string key, CancellationToken ct = default);
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);

        // HashSet operations
        Task HashSetAsync(string hashKey, string field, string value, CancellationToken ct = default);
        Task<string?> HashGetAsync(string hashKey, string field, CancellationToken ct = default);
        Task<Dictionary<string, string>> HashGetAllAsync(string hashKey, CancellationToken ct = default);
        Task<bool> HashRemoveAsync(string hashKey, string field, CancellationToken ct = default);
        Task<bool> HashExistsAsync(string hashKey, string field, CancellationToken ct = default);

        // Lua Script execution
        Task<long> ExecuteLuaScriptAsync(string script, string[] keys, string[] values, CancellationToken ct = default);
    }
}
