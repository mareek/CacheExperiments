namespace CacheExperiments.Caches;

internal class ExpiringCache<TKey, TValue>(int capacity, TimeSpan lifetime): ISimpleCache<TKey, TValue>
    where TKey : notnull
{
#if NET9_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif  

    private readonly Dictionary<TKey, int> _keysIndex = new(capacity);

    private readonly TimeSpan _lifetime = lifetime;

    private readonly ListItem[] _items = new ListItem[capacity];
    private int _firstIndex = -1;
    private int _lastIindex = -1;

    private int _firstAvailbleIndex = capacity - 1;

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        lock (_lock)
        {
            TValue newValue;
            var expirationDate = DateTime.UtcNow.Add(_lifetime);
            if (!_keysIndex.TryGetValue(key, out int index))
            {
                // Key is not present in cache => we generate a new value
                newValue = addValueFactory(key);
                AddOnTop(key, newValue, expirationDate);
            }
            else if (_items[index].ExpirationDate <= DateTime.UtcNow)
            {
                // Key is present in the cache but expired => we generate a new value
                newValue = addValueFactory(key);
                SetValue(index, newValue, expirationDate);
                MoveToTop(index);
            }
            else
            {
                // Key is present in the cache and not expired => we update the stored value
                var oldValue = GetValue(index);
                newValue = updateValueFactory(key, oldValue);
                SetValue(index, newValue, expirationDate);
                MoveToTop(index);
            }

            return newValue;
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        lock (_lock)
        {
            TValue value;
            if (!_keysIndex.TryGetValue(key, out int index))
            {
                // Key is not present in cache => we generate a new value
                var expirationDate = DateTime.UtcNow.Add(_lifetime);
                value = factory(key);
                AddOnTop(key, value, expirationDate);
            }
            else if (_items[index].ExpirationDate < DateTime.UtcNow)
            {
                // Key is present in the cache but expired => we generate a new value
                value = factory(key);
                SetValue(index, value, DateTime.UtcNow.Add(_lifetime));
                MoveToTop(index);
            }
            else
            {
                // Key is present in the cache and not expired => we return the stored value
                value = GetValue(index);
                MoveToTop(index);
            }

            return value;
        }
    }

    private void AddOnTop(TKey key, TValue value, DateTime expirationDate)
    {
        ListItem newItem = new(-1, key, value, expirationDate, _firstIndex);
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

    private void SetValue(int index, TValue newValue, DateTime expirationDate)
        => _items[index] = _items[index].WithValue(newValue, expirationDate);

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

    private readonly struct ListItem(int previousIndex, TKey key, TValue value, DateTime expirationDate, int nextIndex)
    {
        public int PreviousIndex { get; } = previousIndex;
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;
        public DateTime ExpirationDate { get; } = expirationDate;
        public int NextIndex { get; } = nextIndex;

        public ListItem WithPreviousIndex(int index) => new(index, Key, Value, ExpirationDate, NextIndex);
        public ListItem WithNextIndex(int index) => new(PreviousIndex, Key, Value, ExpirationDate, index);
        public ListItem WithValue(TValue value, DateTime expirationDate)
            => new(PreviousIndex, Key, value, expirationDate, NextIndex);
    }
}

public class ExpiringCacheAsync<TKey, TValue>(int capacity, TimeSpan lifetime) : BaseCacheAsync<TKey, TValue>()
    where TKey : notnull
{
    protected override ISimpleCache<TKey, Task<TValue>> BuildInnerCache()
        => new ExpiringCache<TKey, Task<TValue>>(capacity, lifetime);
}