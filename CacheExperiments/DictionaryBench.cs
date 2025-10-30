using System.Data;
using BenchmarkDotNet.Attributes;

namespace CacheExperiments;

[MemoryDiagnoser(true)]
[HideColumns("Error", "StdDev")]
public class DictionaryBench
{
    public enum DicoType
    {
        Dictionary,
        MyDico,
        WorseDic,
        BetterDic,
    }

    private Guid[] _keys = [];
    private Guid[] Keys
    {
        get
        {
            if (_keys.Length != Count)
                _keys = Enumerable.Range(0, Count).Select(i => Guid.NewGuid()).ToArray();

            return _keys;
        }
    }

    //[Params(64, 1024, 16384)]
    [Params(1024)]
    public int Count { get; set; }

    //[Params(0.0625, 0.5, 1, 2)]
    [Params(0.5, 1, 2)]
    public double CapacityFactor { get; set; }

    //[Params(DicoType.Dictionary, DicoType.BetterDic, DicoType.MyDico)]
    [Params(DicoType.BetterDic, DicoType.WorseDic)]
    public DicoType DicType { get; set; }

    private int Capacity => (int)(Count * CapacityFactor);

    private IDictionary<Guid, Guid> CreateDic()
        => DicType switch
        {
            DicoType.Dictionary => new Dictionary<Guid, Guid>(Capacity),
            DicoType.MyDico => new MyDictionary<Guid, Guid>(Capacity),
            DicoType.WorseDic => new WorseDictionary<Guid, Guid>(Capacity),
            DicoType.BetterDic => new BetterDictionary<Guid, Guid>(Capacity),
            _ => throw new ArgumentException($"Unknown dic type : [{DicType}]", nameof(DicType))
        };

    private void Fill(IDictionary<Guid, Guid> dic)
    {
        foreach (var key in Keys)
            dic[key] = key;
    }

    [Benchmark]
    public void FillThenRead10k()
    {
        var dic = CreateDic();
        Fill(dic);

        for (int i = 0; i < 10_000; i++)
        {
            int sum = 0;
            foreach (var key in Keys)
                sum += dic[key].GetHashCode() % 2;
        }
    }

    [Benchmark]
    public void Fill10k()
    {
        var dic = CreateDic();
        for (int i = 0; i < 10_000; i++)
            Fill(dic);
    }

    [Benchmark]
    public void FillThenRemove10k()
    {
        var dic = CreateDic();
        for (int i = 0; i < 10_000; i++)
        {
            Fill(dic);

            foreach (var key in Keys)
                dic.Remove(key);
        }
    }
}
