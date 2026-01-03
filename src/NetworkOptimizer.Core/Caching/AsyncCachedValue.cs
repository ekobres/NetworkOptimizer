namespace NetworkOptimizer.Core.Caching;

/// <summary>
/// Thread-safe async cached value with expiration support.
/// </summary>
/// <typeparam name="T">The type of value to cache.</typeparam>
public class AsyncCachedValue<T> where T : class
{
    private T? _cached;
    private DateTime _cacheTime;
    private readonly TimeSpan _expiry;
    private readonly Func<Task<T>> _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AsyncCachedValue(Func<Task<T>> factory, TimeSpan expiry)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _expiry = expiry;
    }

    public async Task<T> GetAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cached != null && DateTime.UtcNow - _cacheTime < _expiry)
            return _cached;

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (!forceRefresh && _cached != null && DateTime.UtcNow - _cacheTime < _expiry)
                return _cached;

            _cached = await _factory();
            _cacheTime = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate()
    {
        _cached = null;
    }
}
