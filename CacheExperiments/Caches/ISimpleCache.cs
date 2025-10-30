namespace CacheExperiments.Caches
{
    public interface ISimpleCache<TKey, TValue>
    {
        TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory);
        TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);
    }
}