using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the results of the test class should be written to a markdown file.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WriteToMarkdownAttribute : Attribute;