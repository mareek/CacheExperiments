using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CacheExperiments;

public class MyDictionary<TKey, TValue>(int capacity) : IDictionary<TKey, TValue>
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
        var bucket = key.GetHashCode() % _entries.Length;
        if (_entries[bucket] is not Entry entry)
            return false;

        if (KeyEquals(key, entry.Key))
        {
            _entries[bucket] = entry.Next?.Entry;
            return true;
        }

        if (entry.Next is not EntryRef currentRef)
            return false;

        while (currentRef.Entry is Entry entryCur && !KeyEquals(key, entryCur.Key))
        {
            currentRef = entryCur.Next!;
        }

        if (currentRef.Entry is not Entry entryToDelete)
            return false;

        currentRef.Entry = entryToDelete.Next?.Entry;
        return true;
    }

    private TValue Get(TKey key)
        => GetEntry(key) is Entry entry
                ? entry.Value
                : throw new ArgumentException($"There is no key {key}", nameof(key));

    private void Set(TKey key, TValue value)
    {
        var bucket = key.GetHashCode() % _entries.Length;
        if (_entries[bucket] is not Entry entry || KeyEquals(key, entry.Key))
        {
            _entries[bucket] = new(key, value);
            return;
        }

        if (entry.Next is not EntryRef currentRef)
        {
            _entries[bucket] = entry with { Next = new() { Entry = new(key, value) { Next = new() } } };
            return;
        }

        while (currentRef.Entry is Entry entryCur && !KeyEquals(key, entryCur.Key))
        {
            currentRef = entryCur.Next!;
        }

        currentRef.Entry = new(key, value) { Next = new() };
    }

    private Entry? GetEntry(TKey key)
    {
        var bucket = key.GetHashCode() % _entries.Length;
        var entry = _entries[bucket];
        while (entry != null && !KeyEquals(key, entry.Value.Key))
            entry = entry.Value.Next?.Entry;

        return entry;
    }

    private IEnumerable<Entry> EnumerateEntries()
    {
        foreach (var bucket in _entries)
        {
            var entry = bucket;
            while (entry is Entry realEntry)
            {
                yield return realEntry;
                entry = realEntry.Next?.Entry;
            }
        }
    }

    private readonly struct Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;
        public EntryRef? Next { get; init; }

        public KeyValuePair<TKey, TValue> ToKeyValue() => new(Key, Value);
    }

    private class EntryRef
    {
        public Entry? Entry { get; set; }
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
