using System;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Variables;

/// <summary>
/// Base interface for variable providers. This interface is used for type checking
/// but does not define any methods. Use ISailfishVariablesProvider&lt;T&gt; for actual implementation.
/// </summary>
public interface ISailfishVariablesProvider
{
}

/// <summary>
/// Generic interface for providing variables to Sailfish performance tests.
/// Implement this interface to provide a collection of values for a specific type.
/// </summary>
/// <typeparam name="T">The type of variables this provider will supply</typeparam>
public interface ISailfishVariablesProvider<T> : ISailfishVariablesProvider
    where T : IComparable
{
    /// <summary>
    /// Returns a collection of variables of type T that will be used to create test iterations.
    /// Each value returned will result in a separate test execution.
    /// </summary>
    /// <returns>An enumerable collection of variables</returns>
    IEnumerable<T> Variables();
}