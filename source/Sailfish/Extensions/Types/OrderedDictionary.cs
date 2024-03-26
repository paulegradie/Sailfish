using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Extensions.Types;

public class OrderedDictionary : IDictionary<string, string>
{
    private readonly System.Collections.Specialized.OrderedDictionary orderedDictionary = new();

    public string this[string key]
    {
        get => (string)orderedDictionary[key]!;
        set => orderedDictionary[key] = value;
    }

    public ICollection<string> Keys => orderedDictionary.Keys.Cast<string>().ToList();

    public ICollection<string> Values => orderedDictionary.Values.Cast<string>().ToList();

    public int Count => orderedDictionary.Count;

    public bool IsReadOnly => orderedDictionary.IsReadOnly;

    public void Add(string key, string value)
    {
        orderedDictionary.Add(key, value);
    }

    public void Clear()
    {
        orderedDictionary.Clear();
    }

    public bool ContainsKey(string key)
    {
        return orderedDictionary.Contains(key);
    }

    public bool Remove(string key)
    {
        if (!orderedDictionary.Contains(key))
            return false;

        orderedDictionary.Remove(key);
        return true;
    }

    public bool TryGetValue(string key, out string value)
    {
        if (orderedDictionary.Contains(key))
        {
            value = (string)orderedDictionary[key]!;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return (
            from DictionaryEntry entry in orderedDictionary
            select new KeyValuePair<string, string?>((string)entry.Key, (string)entry.Value!)!
        ).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, string> item)
    {
        orderedDictionary.Add(item.Key, item.Value);
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        return orderedDictionary.Contains(item.Key) && orderedDictionary[item.Key]!.Equals(item.Value);
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