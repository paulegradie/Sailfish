using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that the attributed method is responsible for Sailfish method teardown.
/// </summary>
/// <remarks>
/// This attribute should be placed on a single method. Multiple attributes per class are allowed.
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</seealso>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishMethodTeardownAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishMethodTeardownAttribute"/> class with the specified method names.
    /// </summary>
    /// <param name="methodNames">A params array of string names for SailfishMethods this attribute will be applied to.</param>
    public SailfishMethodTeardownAttribute(params string[] methodNames)
    {
        MethodNames = methodNames;
    }

    /// <summary>
    /// Array of method names that the SailfishMethodTeardown method should be executed after
    /// </summary>
    public string[] MethodNames { get; set; }
}