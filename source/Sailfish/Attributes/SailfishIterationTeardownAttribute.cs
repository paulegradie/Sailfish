using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the attributed method is responsible for Sailfish iteration teardown.
/// </summary>
/// <remarks>
///     This attribute should be placed on a single method. Multiple attributes per class are allowed.
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">
///     Sailfish Lifecycle Method
///     Attributes
/// </seealso>
/// <remarks>
///     Initializes a new instance of the <see cref="SailfishIterationTeardownAttribute" /> class
///     with the specified method names.
/// </remarks>
/// <param name="methodNames">The names of the methods to be called during the teardown phase.</param>
/// <remarks>This feature is EXPERIMENTAL</remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SailfishIterationTeardownAttribute(params string[] methodNames) : Attribute, IInnerLifecycleAttribute
{
    /// <summary>
    ///     Array of method names that the SailfishIterationTeardown method should be executed after
    /// </summary>
    public string[] MethodNames { get; } = methodNames.Length > 0 ? methodNames : [];
}