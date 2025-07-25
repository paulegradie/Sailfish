using System;

namespace Sailfish.Contracts.Public.Variables;

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