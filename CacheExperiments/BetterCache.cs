using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheExperiments;

public class BetterCache<Tkey, TValue>(int capacity) : ISimpleCache<Tkey, TValue>
{
    public TValue AddOrUpdate(Tkey key, Func<Tkey, TValue> addValueFactory, Func<Tkey, TValue, TValue> updateValueFactory)
    {
        throw new NotImplementedException();
    }

    public TValue GetOrAdd(Tkey key, Func<Tkey, TValue> factory)
    {
        throw new NotImplementedException();
    }
}
