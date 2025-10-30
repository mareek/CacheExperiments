namespace CacheExperiments.Caches;

/// <summary>
/// A simple & lightweight cache in less than 2⁷ lines of code
/// </summary>
public class BetterCache<TKey, TValue>(int capacity) : ISimpleCache<TKey, TValue>
    where TKey : notnull
{
#if NET9_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif  

    private readonly Dictionary<TKey, int> _keysIndex = new(capacity);

    private readonly ListItem[] _items = new ListItem[capacity];
    private int _firstIndex = -1;
    private int _lastIindex = -1;

    private int _firstAvailbleIndex = capacity - 1;

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        lock (_lock)
        {
            TValue newValue;
            if (_keysIndex.TryGetValue(key, out int index))
            {
                var oldValue = GetValue(index);
                newValue = updateValueFactory(key, oldValue);
                SetValue(index, newValue);
                MoveToTop(index);
            }
            else
            {
                newValue = addValueFactory(key);
                AddOnTop(key, newValue);
            }

            return newValue;
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        lock (_lock)
        {
            TValue value;
            if (_keysIndex.TryGetValue(key, out int index))
            {
                value = GetValue(index);
                MoveToTop(index);
            }
            else
            {
                value = factory(key);
                AddOnTop(key, value);
            }

            return value;
        }
    }

    private void AddOnTop(TKey key, TValue value)
    {
        ListItem newItem = new(-1, key, value, _firstIndex);
        if (_firstAvailbleIndex != -1) // there are still empty slots in _items
        {
            if (_firstIndex == -1) // first insertion
                _lastIindex = _firstAvailbleIndex;
            else
                _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(_firstAvailbleIndex);

            _items[_firstAvailbleIndex] = newItem;
            _firstIndex = _firstAvailbleIndex;
            _firstAvailbleIndex--;
        }
        else
        {
            var lastItem = _items[_lastIindex];
            _keysIndex.Remove(lastItem.Key);
            var newLastIndex = lastItem.PreviousIndex;
            _items[_lastIindex] = newItem;
            _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(_lastIindex);
            _items[newLastIndex] = _items[newLastIndex].WithNextIndex(-1);
            _firstIndex = _lastIindex;
            _lastIindex = newLastIndex;
        }
        _keysIndex[key] = _firstIndex;
    }

    private TValue GetValue(int index) => _items[index].Value;

    private void SetValue(int index, TValue newValue) => _items[index] = _items[index].WithValue(newValue);

    private void MoveToTop(int index)
    {
        if (index == _firstIndex)
            return;

        var item = _items[index];

        _items[item.PreviousIndex] = _items[item.PreviousIndex].WithNextIndex(item.NextIndex);
        if (item.NextIndex != -1)
            _items[item.NextIndex] = _items[item.NextIndex].WithPreviousIndex(item.PreviousIndex);

        _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(index);
        _items[index] = item.WithNextIndex(_firstIndex).WithPreviousIndex(-1);
        _firstIndex = index;
    }

    private readonly struct ListItem(int previousIndex, TKey key, TValue value, int nextIndex)
    {
        public int PreviousIndex { get; } = previousIndex;
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;
        public int NextIndex { get; } = nextIndex;

        public ListItem WithPreviousIndex(int index) => new(index, Key, Value, NextIndex);
        public ListItem WithNextIndex(int index) => new(PreviousIndex, Key, Value, index);
        public ListItem WithValue(TValue value) => new(PreviousIndex, Key, value, NextIndex);
    }
}

public class BetterCacheAsync<TKey, TValue>(int capacity) : BaseCacheAsync<TKey, TValue>(capacity)
    where TKey : notnull
{
    protected override ISimpleCache<TKey, Task<TValue>> BuildInnerCache(int capacity)
        => new BetterCache<TKey, Task<TValue>>(capacity);
}
