using System.Collections.Generic;
using Sailfish.Extensions.Types;
using Xunit;

namespace Tests.Library;

public class OrderedDictionaryTests
{
    [Fact]
    public void Add_KeyValuePair_AddsItem()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };

        Assert.Single(dictionary);
        Assert.Equal("value1", dictionary["key1"]);
    }

    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };

        var value = dictionary["key1"];

        Assert.Equal("value1", value);
    }

    [Fact]
    public void Indexer_Set_UpdatesValue()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };
        dictionary["key1"] = "newValue";

        Assert.Equal("newValue", dictionary["key1"]);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" }, { "key2", "value2" } };

        var keys = dictionary.Keys;

        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" }, { "key2", "value2" } };

        var values = dictionary.Values;

        Assert.Contains("value1", values);
        Assert.Contains("value2", values);
        Assert.Equal(2, values.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" }, { "key2", "value2" } };
        dictionary.Clear();

        Assert.Empty(dictionary);
    }

    [Fact]
    public void ContainsKey_ReturnsTrueForExistingKey()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };

        var contains = dictionary.ContainsKey("key1");

        Assert.True(contains);
    }

    [Fact]
    public void Remove_Key_RemovesItem()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };
        var removed = dictionary.Remove("key1");

        Assert.True(removed);
        Assert.Empty(dictionary);
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrue()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" } };

        var result = dictionary.TryGetValue("key1", out var value);

        Assert.True(result);
        Assert.Equal("value1", value);
    }

    [Fact]
    public void GetEnumerator_EnumeratesAllItems()
    {
        var dictionary = new OrderedDictionary { { "key1", "value1" }, { "key2", "value2" } };

        var count = 0;
        foreach (var item in dictionary) count++;

        Assert.Equal(2, count);
    }

    [Fact]
    public void Add_KeyValuePairInterface_AddsItem()
    {
        var dictionary = new OrderedDictionary();
        var kvp = new KeyValuePair<string, string>("key1", "value1");
        dictionary.Add(kvp);

        Assert.Single(dictionary);
        Assert.Equal("value1", dictionary["key1"]);
    }

    [Fact]
    public void Contains_KeyValuePairInterface_ReturnsTrueForExistingItem()
    {
        var dictionary = new OrderedDictionary();
        var kvp = new KeyValuePair<string, string>("key1", "value1");
        dictionary.Add(kvp);

        var contains = dictionary.Contains(kvp);

        Assert.True(contains);
    }

    [Fact]
    public void Remove_KeyValuePairInterface_RemovesItem()
    {
        var dictionary = new OrderedDictionary();
        var kvp = new KeyValuePair<string, string>("key1", "value1");
        dictionary.Add(kvp);
        var removed = dictionary.Remove(kvp);

        Assert.True(removed);
        Assert.Empty(dictionary);
    }
}