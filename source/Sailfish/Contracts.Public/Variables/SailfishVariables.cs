using System;

namespace Sailfish.Contracts.Public.Variables;

/// <summary>
/// A concrete class for providing type-safe variable instances to Sailfish performance tests.
/// This class provides a cleaner alternative to the interface-based ISailfishVariables pattern
/// by allowing direct declaration of variable types and their providers.
/// 
/// Usage:
/// <code>
/// public SailfishVariables&lt;DatabaseConfiguration, DatabaseConfigurationProvider&gt; DatabaseConfig { get; set; }
/// </code>
/// 
/// This system works alongside the existing SailfishVariableAttribute and ISailfishVariables
/// systems and provides better usability with less boilerplate code.
/// </summary>
/// <typeparam name="T">The data type that will be used as test variables</typeparam>
/// <typeparam name="TProvider">The provider type that implements ISailfishVariablesProvider&lt;T&gt;</typeparam>
public class SailfishVariables<T, TProvider> : IComparable
    where T : IComparable
    where TProvider : ISailfishVariablesProvider<T>, new()
{
    /// <summary>
    /// The actual variable value that will be set during test execution
    /// </summary>
    public T Value { get; set; } = default!;

    /// <summary>
    /// Implicit conversion to T for seamless usage in test methods
    /// </summary>
    /// <param name="sailfishVar">The SailfishVariables instance</param>
    /// <returns>The underlying value</returns>
    public static implicit operator T(SailfishVariables<T, TProvider> sailfishVar)
    {
        return sailfishVar.Value;
    }

    /// <summary>
    /// Implicit conversion from T for easy assignment during test execution
    /// </summary>
    /// <param name="value">The value to wrap</param>
    /// <returns>A new SailfishVariables instance</returns>
    public static implicit operator SailfishVariables<T, TProvider>(T value)
    {
        return new SailfishVariables<T, TProvider> { Value = value };
    }

    /// <summary>
    /// Compares this instance with another object for ordering
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>A value indicating the relative order</returns>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            SailfishVariables<T, TProvider> other => Value.CompareTo(other.Value),
            T directValue => Value.CompareTo(directValue),
            _ => 1
        };
    }

    /// <summary>
    /// Returns a string representation of the underlying value
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>True if the objects are equal</returns>
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            SailfishVariables<T, TProvider> other => Equals(Value, other.Value),
            T directValue => Equals(Value, directValue),
            _ => false
        };
    }

    /// <summary>
    /// Returns a hash code for the current object
    /// </summary>
    /// <returns>A hash code for the current object</returns>
    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }
}
