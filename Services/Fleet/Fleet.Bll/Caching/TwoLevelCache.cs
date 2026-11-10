using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Fleet.Bll.Caching;

public interface ITwoLevelCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, string reason, CancellationToken ct = default);
}

// Дворівневий кеш L1 (пам'ять) -> L2 (Redis) -> БД
public class TwoLevelCache : ITwoLevelCache
{
    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _redis;
    private readonly CacheMetrics _metrics;
    private readonly ILogger<TwoLevelCache> _logger;

    public TwoLevelCache(
        IMemoryCache memory,
        IDistributedCache redis,
        CacheMetrics metrics,
        ILogger<TwoLevelCache> logger)
    {
        _memory = memory;
        _redis = redis;
        _metrics = metrics;
        _logger = logger;
    }

    // Cache-aside: перевірка кешу -> БД при промаху -> запис у кеш
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? l1Ttl = null,
        TimeSpan? l2Ttl = null,
        CancellationToken ct = default)
    {
        // Рівень L1 (IMemoryCache)
        if (_memory.TryGetValue(key, out T? memoryValue))
        {
            _metrics.Hit("L1");
            _logger.LogDebug("Cache hit {Key}", key);
            return memoryValue;
        }

        _metrics.Miss("L1");
        _logger.LogDebug("Cache miss {Key}", key);

        // Рівень L2 (IDistributedCache / Redis)
        byte[]? redisBytes = null;
        try
        {
            redisBytes = await _redis.GetAsync(key, ct);
        }
        catch (Exception ex)
        {
            // Graceful degradation кешу Redis (Критерій 5)
            _logger.LogWarning(ex, "Redis unavailable, falling back to database for {Key}", key);
        }

        if (redisBytes is not null)
        {
            _metrics.Hit("L2");
            _logger.LogDebug("Cache hit {Key}", key);

            var deserialized = JsonSerializer.Deserialize<T>(redisBytes);
            if (deserialized is not null)
            {
                // L1 TTL коротший за L2
                SetMemory(key, deserialized, l1Ttl ?? TimeSpan.FromMinutes(1));
                return deserialized;
            }
        }
        else
        {
            _metrics.Miss("L2");
            _logger.LogDebug("Cache miss {Key}", key);
        }

        // Читання з БД
        var dbValue = await factory();
        if (dbValue is null)
        {
            return default;
        }

        // Рівні заповнюються на зворотному шляху: спершу L2, потім L1
        try
        {
            var l2Options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2Ttl ?? TimeSpan.FromMinutes(10)
            };
            await _redis.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(dbValue), l2Options, ct);
        }
        catch (Exception ex)
        {
            // Graceful degradation кешу Redis (Критерій 5)
            _logger.LogWarning(ex, "Redis unavailable, value for {Key} not cached", key);
        }

        // L1 TTL коротший за L2
        SetMemory(key, dbValue, l1Ttl ?? TimeSpan.FromMinutes(1));
        return dbValue;
    }

    // Інвалідація кешу одразу після запису
    public async Task RemoveAsync(string key, string reason, CancellationToken ct = default)
    {
        _memory.Remove(key);
        try
        {
            await _redis.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while removing {Key}", key);
        }

        _logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
    }

    private void SetMemory<T>(string key, T value, TimeSpan ttl)
    {
        // SlidingExpiration менший за AbsoluteExpiration; AbsoluteExpiration = ttl (L1 TTL коротший за L2)
        var sliding = TimeSpan.FromTicks(Math.Min(ttl.Ticks, TimeSpan.FromMinutes(1).Ticks));
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = sliding,
            AbsoluteExpirationRelativeToNow = ttl
        };
        _memory.Set(key, value, options);
    }
}

