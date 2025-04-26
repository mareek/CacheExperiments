namespace CacheExperiments.Test;

public class BetterCacheTest : SimpleCacheTest<BetterCache<string, int>>
{
    protected override BetterCache<string, int> NewCache(int capacity) => new(capacity);
}
