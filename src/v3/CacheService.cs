using StackExchange.Redis;
using System.Text.Json;

namespace v2
{
    public class CacheService
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _redis;
        private const int DefaultExpirationMinutes = 60;

        public CacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (!value.HasValue)
                    return null;

                return JsonSerializer.Deserialize<T>(value!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to get cache key '{key}': {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetStringAsync(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to get cache key '{key}': {ex.Message}");
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, serializedValue, expiration ?? TimeSpan.FromMinutes(DefaultExpirationMinutes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to set cache key '{key}': {ex.Message}");
            }
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
        {
            try
            {
                await _database.StringSetAsync(key, value, expiration ?? TimeSpan.FromMinutes(DefaultExpirationMinutes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to set cache key '{key}': {ex.Message}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to remove cache key '{key}': {ex.Message}");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to check cache key '{key}': {ex.Message}");
                return false;
            }
        }

        // 为短链接优化的缓存方法
        public async Task<(long id, string url, long expire)?> GetShortLinkAsync(string alias)
        {
            try
            {
                var cacheKey = $"shortlink:{alias}";
                var cachedValue = await GetStringAsync(cacheKey);
                if (cachedValue != null)
                {
                    var parts = cachedValue.Split('|');
                    if (parts.Length == 3 && 
                        long.TryParse(parts[0], out var id) && 
                        long.TryParse(parts[2], out var expire))
                    {
                        return (id, parts[1], expire);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to get shortlink from cache '{alias}': {ex.Message}");
                return null;
            }
        }

        public async Task SetShortLinkAsync(string alias, long id, string url, long expire, TimeSpan? expiration = null)
        {
            try
            {
                var cacheKey = $"shortlink:{alias}";
                var cacheValue = $"{id}|{url}|{expire}";
                
                // 如果短链接本身有过期时间，使用较短的时间作为缓存过期时间
                var cacheExpiration = expiration ?? TimeSpan.FromMinutes(DefaultExpirationMinutes);
                if (expire > 0)
                {
                    var expireDateTime = new DateTime(expire);
                    var linkExpiration = expireDateTime.Subtract(DateTime.UtcNow);
                    if (linkExpiration > TimeSpan.Zero && linkExpiration < cacheExpiration)
                    {
                        cacheExpiration = linkExpiration;
                    }
                }
                
                await SetStringAsync(cacheKey, cacheValue, cacheExpiration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to set shortlink cache '{alias}': {ex.Message}");
            }
        }

        public async Task RemoveShortLinkAsync(string alias)
        {
            try
            {
                var cacheKey = $"shortlink:{alias}";
                await RemoveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to remove shortlink cache '{alias}': {ex.Message}");
            }
        }
    }
}
