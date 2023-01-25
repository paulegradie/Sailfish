using System;
using Sailfish.Execution;

namespace Sailfish.AdapterUtils;

/// <summary>
/// When implementing fixture classes, you need to inherit from this interface in order
/// to ensure your tests are correctly disposed, and that Sailfish has a contract for
/// how to resolve types you need provided to the tests
/// </summary>
public interface ISailfishFixtureDependency : ITypeResolver, IDisposable
{
}