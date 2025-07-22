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
public interface ISailfishVariablesProvider<out T> : ISailfishVariablesProvider
    where T : IComparable
{
    /// <summary>
    /// Returns a collection of variables, of type T that will be used to create test iterations.
    /// Each value returned will result in a separate test execution.
    /// </summary>
    /// <returns>An enumerable collection of variables</returns>
    IEnumerable<T> Variables();
}

/// <summary>
/// Interface for providing type-safe variable instances to Sailfish performance tests.
/// This interface separates data type concerns from variable generation concerns by using
/// a dedicated provider type to generate test instances.
///
/// This system works alongside the existing SailfishVariableAttribute and ISailfishComplexVariableProvider
/// systems and provides better separation of concerns.
/// </summary>
/// <typeparam name="TType">The data type that will be used as test variables</typeparam>
/// <typeparam name="TTypeProvider">The provider type that implements ISailfishVariablesProvider&lt;TType&gt;</typeparam>
public interface ISailfishVariables<TType, TTypeProvider> : IComparable
    where TType : IComparable
    where TTypeProvider : ISailfishVariablesProvider<TType>, new()
{
    // Marker interface - the generic parameters provide all the type information needed
    // The framework will use TTypeProvider to generate instances of TType
}
