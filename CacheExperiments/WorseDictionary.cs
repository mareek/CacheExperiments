using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CacheExperiments;

public class WorseDictionary<TKey, TValue>(int capacity) : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private static readonly EqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;
    private static readonly EqualityComparer<TValue> ValueComparer = EqualityComparer<TValue>.Default;
    
    private static bool KeyEquals(TKey left, TKey right) => KeyComparer.Equals(left, right);

    private readonly Entry?[] _entries = new Entry?[capacity];

    public TValue this[TKey key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    public void Add(TKey key, TValue value)
    {
        if (ContainsKey(key))
            throw new ArgumentException($"There is already a key {key}", nameof(key));

        Set(key, value);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        bool result = ContainsKey(key);

        value = result ? Get(key) : default;
        return result;
    }

    public bool ContainsKey(TKey key) => GetEntry(key) is not null;

    public bool Remove(TKey key)
    {
        var bucketIndex = GetBucketIndex(key);
        if (_entries[bucketIndex] is not Entry entry)
            return false;

        if (KeyEquals(key, entry.Key))
        {
            _entries[bucketIndex] = entry.Next is EntryRef entryRef
                                         ? entryRef.ToEntry() with { Next = entryRef.Next }
                                         : null;
            return true;
        }

        if (entry.Next is not EntryRef currentRef)
            return false;

        while (true)
        {
            if (KeyEquals(key, currentRef.Key))
            {
                var wasSet = currentRef.IsSet;
                currentRef.IsSet = false;
                return wasSet;
            }

            if (currentRef.Next is null)
                return false;

            currentRef = currentRef.Next;
        }
    }

    private TValue Get(TKey key)
        => GetEntry(key) is Entry entry
                ? entry.Value
                : throw new ArgumentException($"There is no key {key}", nameof(key));

    private void Set(TKey key, TValue value)
    {
        var bucket = GetBucketIndex(key);
        if (_entries[bucket] is not Entry entry || KeyEquals(key, entry.Key))
        {
            _entries[bucket] = new(key, value);
            return;
        }

        if (entry.Next is not EntryRef currentRef)
        {
            _entries[bucket] = entry with { Next = new(key, value) };
            return;
        }

        EntryRef? availableSlot = null;
        while (true)
        {
            if (KeyEquals(key, currentRef.Key))
            {
                currentRef.Value = value;
                currentRef.IsSet = true;
                return;
            }

            if (!currentRef.IsSet)
                availableSlot = currentRef;

            if (currentRef.Next is null)
            {
                if (availableSlot is not null)
                    availableSlot.Set(key, value);
                else
                    currentRef.Next = new(key, value);

                return;
            }

            currentRef = currentRef.Next;
        }
    }

    private int GetBucketIndex(TKey key) => Math.Abs(key.GetHashCode()) % _entries.Length;

    private Entry? GetBucket(TKey key) => _entries[Math.Abs(key.GetHashCode()) % _entries.Length];

    private Entry? GetEntry(TKey key)
        => EnumerateBucket(GetBucket(key)).Cast<Entry?>()
                                          .FirstOrDefault(e => e.HasValue && KeyEquals(key, e.Value.Key));

    private IEnumerable<Entry> EnumerateEntries()
    {
        foreach (var bucket in _entries)
            foreach (var entry in EnumerateBucket(bucket))
                yield return entry;
    }

    private IEnumerable<Entry> EnumerateBucket(Entry? bucket)
    {
        if (bucket is not Entry entry)
            yield break;

        yield return entry;

        var entryRef = entry.Next;
        while (entryRef is not null)
        {
            if (entryRef.IsSet)
                yield return entryRef.ToEntry();

            entryRef = entryRef.Next;
        }
    }

    private readonly struct Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;
        public EntryRef? Next { get; init; }

        public KeyValuePair<TKey, TValue> ToKeyValue() => new(Key, Value);
    }

    private class EntryRef(TKey key, TValue value)
    {
        public TKey Key { get; private set; } = key;
        public TValue Value { get; set; } = value;
        public bool IsSet { get; set; } = true;
        public EntryRef? Next { get; set; }

        public void Set(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            IsSet = true;
        }

        public Entry ToEntry() => new(Key, Value);
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys
      => this.EnumerateEntries().Select(e => e.Key).ToArray();

    ICollection<TValue> IDictionary<TKey, TValue>.Values
        => this.EnumerateEntries().Select(e => e.Value).ToArray();

    int ICollection<KeyValuePair<TKey, TValue>>.Count
        => this.EnumerateEntries().Count();

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    void ICollection<KeyValuePair<TKey, TValue>>.Clear()
    {
        for (int i = 0; i < _entries.Length; i++)
            _entries[i] = default;
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => GetEntry(item.Key) is Entry entry && ValueComparer.Equals(entry.Value, item.Value);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => this.Contains(item) && Remove(item.Key);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
         => EnumerateEntries().Select(e => e.ToKeyValue()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
         => EnumerateEntries().Select(e => e.ToKeyValue()).GetEnumerator();
}
