using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that the attributed method is responsible for Sailfish iteration teardown.
/// </summary>
/// <remarks>
/// This attribute should be placed on a single method. Multiple attributes per class are allowed.
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</seealso>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishIterationTeardownAttribute : Attribute, IInnerLifecycleAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishIterationTeardownAttribute"/> class
    /// with the specified method names.
    /// </summary>
    /// <param name="methodNames">The names of the methods to be called during the teardown phase.</param>
    public SailfishIterationTeardownAttribute(params string[] methodNames)
    {
        MethodNames = methodNames;
    }

    /// <summary>
    /// Array of method names that the SailfishIterationTeardown method should be executed after
    /// </summary>
    public string[] MethodNames { get; }
}