using Sailfish.Execution;

namespace Sailfish.Registration;

/// <summary>
/// Implement this to specify type providers that should be inlcluded in Sailfish's pool
/// of ITypeResolvers 
/// </summary>
public interface IProvideTypesToSailfish : ITypeResolver
{
}