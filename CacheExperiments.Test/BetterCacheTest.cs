using NFluent;

namespace CacheExperiments.Test;

public class BetterCacheTest : SimpleCacheTest<BetterCache<string, int>>
{
    protected override BetterCache<string, int> NewCache(int capacity) => new(capacity);
}

public class BetterCacheAsyncTest
{
    private const int runCount = 1000;

    [Fact]
    public async Task CheckThatAsyncCacheDoOnlyOneLoadOnAsyncGetOrAdd()
    {
        int loadCount = 0;
        BetterCacheAsync<int, int> cache = new(10);

        Task<int>[] results = new Task<int>[runCount];
        for (int i = 0; i < runCount; i++)
            results[i] = cache.GetOrAddAsync(0, LongLoad);

        await Task.WhenAll(results);

        Check.That(loadCount).IsEqualTo(1);

        async Task<int> LongLoad(int key)
        {
            await Task.Delay(100);
            return Interlocked.Increment(ref loadCount);
        }
    }

    [Fact]
    public async Task CheckThatAsyncCacheDoOnlyOneLoadOnSyncGetOrAdd()
    {
        int loadCount = 0;
        BetterCacheAsync<int, int> cache = new(10);

        Task<int>[] results = new Task<int>[runCount];
        for (int i = 0; i < runCount; i++)
            results[i] = LongLoad(0);

        await Task.WhenAll(results);

        Check.That(loadCount).IsEqualTo(1);

        async Task<int> LongLoad(int key)
        {
            await Task.Delay(100);
            return await cache.GetOrAddAsync(key, Load);
        }

        int Load(int key) => Interlocked.Increment(ref loadCount);
    }

    [Fact]
    public async Task CheckThatAsyncCacheDoOnlyOneLoadOnAsyncAddOrUpdate()
    {
        int loadCount = 0;
        int updateCount = 0;
        BetterCacheAsync<int, int> cache = new(10);

        Task<int>[] results = new Task<int>[runCount];
        for (int i = 0; i < runCount; i++)
            results[i] = cache.AddOrUpdateAsync(0, LongLoad, (key, value) => Task.FromResult(Interlocked.Increment(ref updateCount)));

        await Task.WhenAll(results);

        Check.That(loadCount).IsEqualTo(1);
        Check.That(updateCount).IsEqualTo(runCount - 1);

        async Task<int> LongLoad(int key)
        {
            await Task.Delay(100);
            return Interlocked.Increment(ref loadCount);
        }
    }

    [Fact]
    public async Task CheckThatAsyncCacheDoOnlyOneLoadOnSyncAddOrUpdate()
    {
        int loadCount = 0;
        int updateCount = 0;
        BetterCacheAsync<int, int> cache = new(10);

        Task<int>[] results = new Task<int>[runCount];
        for (int i = 0; i < runCount; i++)
            results[i] = LongLoad(0);

        await Task.WhenAll(results);

        Check.That(loadCount).IsEqualTo(1);

        async Task<int> LongLoad(int key)
        {
            await Task.Delay(100);
            return await cache.AddOrUpdateAsync(key,
                                                key => Interlocked.Increment(ref loadCount),
                                                (key, value) => Interlocked.Increment(ref updateCount));
        }
    }
}
