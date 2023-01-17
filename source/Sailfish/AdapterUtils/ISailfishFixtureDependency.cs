using System;
using Sailfish.Execution;

namespace Sailfish.AdapterUtils;

/// <summary>
/// When implementing fixture classes, you need to inherit from this interface in order
/// to ensure your tests are correctly disposed
/// </summary>
public interface ISailfishFixtureDependency : ITypeResolver, IDisposable
{
}