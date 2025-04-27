using BenchmarkDotNet.Attributes;

namespace CacheExperiments;

[MemoryDiagnoser(false)]
public class CacheBench
{

    [Params(64, 1024, 16384)]
    public int CacheSize { get; set; }

    [Benchmark]
    public void BestCaseQD()
    {
        QDCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key, _ => 1);
        }
    }

    [Benchmark]
    public void BestCaseBetter()
    {
        BetterCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key, _ => 1);
        }
    }

    [Benchmark]
    public void InBetweenCaseQD()
    {
        QDCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key + (i % (CacheSize / 2)), _ => 1);
        }
    }

    [Benchmark]
    public void InBetweenCaseBetter()
    {
        BetterCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key + (i % (CacheSize / 2)), _ => 1);
        }
    }

    [Benchmark]
    public void WorstCaseQD()
    {
        QDCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key + i, _ => 1);
        }
    }

    [Benchmark]
    public void WorstCaseBetter()
    {
        BetterCache<long, int> cache = new(CacheSize);
        long key = 35;
        for (int i = 0; i < 16384; i++)
        {
            cache.GetOrAdd(key + i, _ => 1);
        }
    }
}

