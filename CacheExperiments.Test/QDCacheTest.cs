using CacheExperiments.Caches;

namespace CacheExperiments.Test;

public class QDCacheTest : SimpleCacheTest<QDCache<string, int>>
{
    protected override QDCache<string, int> NewCache(int capacity) => new(capacity);
}
