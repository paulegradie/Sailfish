using System;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Variables;

/// <summary>
/// Interface for providing complex object instances to Sailfish performance tests.
/// This interface allows properties to serve dual purposes:
/// 1. Hold the iteration data for the performance test
/// 2. Provide instances of complex objects at runtime
/// 
/// This system works alongside the existing SailfishVariableAttribute system
/// and provides a cleaner alternative to passing Types to constructors.
/// 
/// NOTE: This interface is being deprecated in favor of ISailfishVariables&lt;TType, TTypeProvider&gt;
/// which provides better separation of concerns.
/// </summary>
/// <typeparam name="T">The type that implements this interface and will be used as test variables</typeparam>
public interface ISailfishComplexVariableProvider<T> : IComparable
    where T : ISailfishComplexVariableProvider<T>, IComparable
{
    /// <summary>
    /// Returns a collection of instances of type T that will be used to create test iterations.
    /// Each instance returned will result in a separate test execution where the test class
    /// property will be set to that instance.
    /// </summary>
    /// <returns>An enumerable collection of T instances</returns>
    static abstract IEnumerable<T> GetVariableInstances();
}
