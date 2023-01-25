namespace Sailfish.Registration;

/// <summary>
/// Use this interface to specify a type as a type that should be registered.
/// The Type discovered automatically and added ot the available registrations
/// Note: This will be overriden by any types registered by the RegistrationCallback
/// when using the Sailfish.Run method 
/// </summary>
public interface ISailfishDependency
{
}

// /// <summary>
// /// This is just a hack to get around a container building bug when this interface doesn't have any implementations
// /// </summary>
// internal class SailfishInternalMockDep342sfs : ISailfishDependency
// {
//     
// }