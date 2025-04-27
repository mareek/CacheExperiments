namespace CacheExperiments;

public class BetterCache<TKey, TValue>(int capacity) : ISimpleCache<TKey, TValue>
{
    private readonly LinkedList _list = new(capacity);

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        TValue newValue;
        if (_list.TryGetKeyIndex(key, out int index))
        {
            var oldValue = _list.GetValue(index);
            newValue = updateValueFactory(key, oldValue);
            _list.SetValue(index, newValue);
            _list.MoveToTop(index);
        }
        else
        {
            newValue = addValueFactory(key);
            _list.AddOnTop(key, newValue);
        }

        return newValue;
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        TValue value;
        if (_list.TryGetKeyIndex(key, out int index))
        {
            value = _list.GetValue(index);
            _list.MoveToTop(index);
        }
        else
        {
            value = factory(key);
            _list.AddOnTop(key, value);
        }

        return value;
    }

    private class LinkedList(int capacity)
    {
        private readonly ListItem[] _items = Enumerable.Range(0, capacity)
                                                       .Select(_ => new ListItem(-1, default!, default!, -1))
                                                       .ToArray();
        private int _firstIndex = -1;
        private int _lastIindex = -1;
        private int _firstAvailbleIndex = 0;

        public void AddOnTop(TKey key, TValue value)
        {
            ListItem newItem = new(-1, key, value, _firstIndex);
            if (_firstAvailbleIndex < _items.Length)
            {
                if (_firstIndex == -1)
                    _lastIindex = _firstAvailbleIndex;
                else
                    _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(_firstAvailbleIndex);

                _items[_firstAvailbleIndex] = newItem;
                _firstIndex = _firstAvailbleIndex;
                _firstAvailbleIndex++;
            }
            else
            {
                var newLastIndex = _items[_lastIindex].PreviousIndex;
                _items[_lastIindex] = newItem;
                _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(_lastIindex);
                _items[newLastIndex] = _items[newLastIndex].WithNextIndex(-1);
                _firstIndex = _lastIindex;
                _lastIindex = newLastIndex;
            }
        }

        public TValue GetValue(int index) => _items[index].Value;

        public void SetValue(int index, TValue newValue) => _items[index] = _items[index].WithValue(newValue);

        public void MoveToTop(int index)
        {
            if (index == _firstIndex)
                return;

            var item = _items[index];
            if (item.PreviousIndex != -1)
                _items[item.PreviousIndex] = _items[item.PreviousIndex].WithNextIndex(item.NextIndex);

            if (item.NextIndex != -1)
                _items[item.NextIndex] = _items[item.NextIndex].WithPreviousIndex(item.PreviousIndex);

            _items[_firstIndex] = _items[_firstIndex].WithPreviousIndex(index);
            _items[index] = item.WithNextIndex(_firstIndex).WithPreviousIndex(-1);
            _firstIndex = index;
        }

        public bool TryGetKeyIndex(TKey key, out int index)
        {
            index = _firstIndex;
            while (index != -1)
            {
                var currentItem = _items[index];
                if (EqualityComparer<TKey>.Default.Equals(key, currentItem.Key))
                    return true;

                index = currentItem.NextIndex;
            }

            return false;
        }

        private struct ListItem(int previousIndex, TKey key, TValue value, int nextIndex)
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
}
