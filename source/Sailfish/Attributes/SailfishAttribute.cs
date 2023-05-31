using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that a class is a Sailfish test class.
/// </summary>
/// <seealso cref="SailfishMethodAttribute"/>
/// <seealso cref="SailfishIterationSetupAttribute"/>
/// <seealso cref="SailfishIterationTeardownAttribute"/>
/// <seealso cref="SailfishGlobalSetupAttribute"/>
/// <seealso cref="SailfishGlobalTeardownAttribute"/>
/// <seealso cref="SailfishVariableAttribute"/>
/// <seealso cref="SuppressConsoleAttribute"/>
/// <seealso cref="WriteToCsvAttribute"/>
/// <seealso cref="WriteToMarkdownAttribute"/>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/the-sailfish-attribute">The Sailfish Attributes</seealso>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SailfishAttribute : Attribute
{
    private const int DefaultNumIterations = 3;
    private const int DefaultNumWarmupIterations = 3;

    internal SailfishAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishAttribute"/> class with the specified number of iterations and warm-up iterations.
    /// </summary>
    /// <param name="numIterations">The number of times each SailfishMethod will be iterated.</param>
    /// <param name="numWarmupIterations">The number of times each SailfishMethod will be iterated without being timed before executing <paramref name="numIterations"/> with tracking.</param>
    /// <remarks>
    /// Each iteration includes the invocation of three methods: SailfishIterationSetup, SailfishMethod, and SailfishIterationTeardown, in that order.
    /// </remarks>
    public SailfishAttribute(
        [Range(2, int.MaxValue)] int numIterations = DefaultNumIterations,
        [Range(0, int.MaxValue)] int numWarmupIterations = DefaultNumWarmupIterations)
    {
        NumIterations = numIterations;
        NumWarmupIterations = numWarmupIterations;
    }

    /// <summary>
    /// Gets or sets the number of iterations for each SailfishMethod.
    /// </summary>
    /// <value>The number of iterations.</value>
    [Range(2, int.MaxValue)]
    public int NumIterations { get; set; }

    /// <summary>
    /// Gets or sets the number of warm-up iterations for each SailfishMethod.
    /// </summary>
    /// <value>The number of warm-up iterations.</value>
    [Range(0, int.MaxValue)]
    public int NumWarmupIterations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Sailfish test is disabled.
    /// </summary>
    /// <value><c>true</c> if the test is disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; set; }
}