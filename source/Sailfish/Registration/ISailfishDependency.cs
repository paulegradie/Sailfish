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
