using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Extensions.Types;

public class OrderedDictionary : IDictionary<string, string>
{
    private readonly System.Collections.Specialized.OrderedDictionary _orderedDictionary = new();

    public string this[string key]
    {
        get => (string)_orderedDictionary[key]!;
        set => _orderedDictionary[key] = value;
    }

    public ICollection<string> Keys => _orderedDictionary.Keys.Cast<string>().ToList();

    public ICollection<string> Values => _orderedDictionary.Values.Cast<string>().ToList();

    public int Count => _orderedDictionary.Count;

    public bool IsReadOnly => _orderedDictionary.IsReadOnly;

    public void Add(string key, string value)
    {
        _orderedDictionary.Add(key, value);
    }

    public void Clear()
    {
        _orderedDictionary.Clear();
    }

    public bool ContainsKey(string key)
    {
        return _orderedDictionary.Contains(key);
    }

    public bool Remove(string key)
    {
        if (!_orderedDictionary.Contains(key))
            return false;

        _orderedDictionary.Remove(key);
        return true;
    }

    public bool TryGetValue(string key, out string value)
    {
        if (_orderedDictionary.Contains(key))
        {
            value = (string)_orderedDictionary[key]!;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return (
            from DictionaryEntry entry in _orderedDictionary
            select new KeyValuePair<string, string?>((string)entry.Key, (string)entry.Value!)!
        ).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, string> item)
    {
        _orderedDictionary.Add(item.Key, item.Value);
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        return _orderedDictionary.Contains(item.Key) && _orderedDictionary[item.Key]!.Equals(item.Value);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        foreach (var entry in this) array[arrayIndex++] = entry;
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
        return Contains(item) && Remove(item.Key);
    }
}