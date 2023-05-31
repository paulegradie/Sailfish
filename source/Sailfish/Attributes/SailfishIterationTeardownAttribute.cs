using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that the attributed method is a Sailfish iteration teardown method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishIterationTeardownAttribute : Attribute
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




/// <summary>
/// Attribute to be placed on a single method responsible for Iteration Teardown.
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <param name="methodNames">A params array (comma delimited values) of string names for SailfishMethods this attribute will be applied to</param>
/// <note>The IterationTeardown method is called once after each invocation of the SailfishMethod, which in turn is executed numIterations + numWarmupIterations times. Method will be executed before each each SailfishMethod that is indicated in its params array. If no params are specified, all methods are included. If multiple methods contain overlapping method names, they will both be applied, in which case, order of method invocation is determined alphabetically</note>
