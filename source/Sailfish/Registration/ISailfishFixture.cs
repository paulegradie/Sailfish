namespace Sailfish.Registration;

/// <summary>
///     Like xunit, this provides a way to specify registrations that you can resolve in your sailfish tests
///     This is provided for the adapter
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISailfishFixture<T> where T : class
{
}