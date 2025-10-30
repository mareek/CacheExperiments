using CacheExperiments.Caches;
using NFluent;

namespace CacheExperiments.Test;

public abstract class SimpleCacheTest<TCache> where TCache : ISimpleCache<string, int>
{
    protected abstract TCache NewCache(int capacity);

    [Fact]
    public void EnsureBasicCacheWorks()
    {
        int factoryCount = 0;
        TCache cache = NewCache(5);

        for (int i = 0; i < 10; i++)
        {
            var first = cache.GetOrAdd("first", _ => ++factoryCount);
            Check.That(first).Is(1);
            var second = cache.GetOrAdd("second", _ => ++factoryCount);
            Check.That(second).Is(2);
        }
    }

    [Fact]
    public void EnsureCacheWorksWithOverflow()
    {
        int factoryCount = 0;
        TCache cache = NewCache(5);

        for (int i = 0; i < 10; i++)
        {
            var other = cache.GetOrAdd($"{i}", _ => ++factoryCount);
            Check.That(other).Is(factoryCount);
            var second = cache.GetOrAdd("second", _ => ++factoryCount);
            Check.That(second).Is(2);
        }

        Check.That(factoryCount).Is(11);
    }

    [Fact]
    public void EnsureAddOrUpdateMethodWorks()
    {
        TCache cache = NewCache(5);

        for (int i = 0; i < 10; i++)
        {
            var other = cache.AddOrUpdate($"{i}", _ => 0, (_, v) => v + 1);
            Check.That(other).Is(0);
            var second = cache.AddOrUpdate("second", _ => 0, (_, v) => v + 1);
            Check.That(second).Is(i);
        }

        var first = cache.AddOrUpdate("0", _ => 0, (_, v) => v + 1);
        Check.That(first).Is(0);
    }
}
