using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that no console outputs are desired for the test class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SuppressConsoleAttribute : Attribute;