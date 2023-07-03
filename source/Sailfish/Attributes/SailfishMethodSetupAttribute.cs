using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that the attributed method is responsible for Sailfish method setup.
/// </summary>
/// <remarks>
/// This attribute should be placed on a single method. Multiple attributes per class are allowed.
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</seealso>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishMethodSetupAttribute : Attribute, IInnerLifecycleAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishMethodSetupAttribute"/> class with the specified method names.
    /// </summary>
    /// <param name="methodNames">A params array of string names for SailfishMethods this attribute will be applied to.</param>
    /// <remarks>This feature is EXPERIMENTAL</remarks>
    public SailfishMethodSetupAttribute(params string[] methodNames)
    {
        MethodNames = methodNames.Length > 0 ? methodNames : Array.Empty<string>();
    }

    /// <summary>
    /// Array of method names that the SailfishIterationTeardown method should be executed after
    /// </summary>
    public string[] MethodNames { get; set; }
}
