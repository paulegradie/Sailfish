using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the attributed method is responsible for Sailfish global setup.
/// </summary>
/// <remarks>
///     This attribute should be placed on a single method. Only one method is allowed per Sailfish test class.
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">
///     Sailfish Lifecycle Method
///     Attributes
/// </seealso>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class SailfishGlobalSetupAttribute : Attribute
{
}