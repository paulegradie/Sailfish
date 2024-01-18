using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the results of the test class should be written to a file in CSV format.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WriteToCsvAttribute : Attribute
{
}