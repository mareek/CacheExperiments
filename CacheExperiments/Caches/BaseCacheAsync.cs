namespace CacheExperiments.Caches;

public abstract class BaseCacheAsync<TKey, TValue>
    where TKey : notnull
{
    private readonly ISimpleCache<TKey, Task<TValue>> _innerCache;

    protected BaseCacheAsync(int capacity) => _innerCache = BuildInnerCache(capacity);

    protected abstract ISimpleCache<TKey, Task<TValue>> BuildInnerCache(int capacity);

    public async Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        => await _innerCache.AddOrUpdate(key,
                                         k => Task.FromResult(addValueFactory(k)),
                                         async (k, v) => updateValueFactory(k, await v));

    public async Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, Task<TValue>> addValueFactoryAsync, Func<TKey, TValue, Task<TValue>> updateValueFactoryAsync)
        => await _innerCache.AddOrUpdate(key,
                                         addValueFactoryAsync,
                                         async (k, v) => await updateValueFactoryAsync(k, await v));

    public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, TValue> factory)
        => await GetOrAddAsync(key, k => Task.FromResult(factory(k)));

    public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> factoryAsync)
        => await _innerCache.GetOrAdd(key, factoryAsync);
}
