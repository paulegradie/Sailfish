using System;

namespace Sailfish.Attributes;

/// <summary>
/// Attribute to be placed on a single method responsible for Global Teardown. Only a single attribute per class is allowed.
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <remarks>The decorated method is invoked once per Sailfish test class and is the last method to be invoked</remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SailfishGlobalTeardownAttribute : Attribute
{
}