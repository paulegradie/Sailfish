using System;

namespace Sailfish.Attributes;

/// <summary>
/// Attribute to be placed on one or more methods whose execution time will be tracked
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <note>The SailfishMethod is called NumWarmupIterations times without time tracking, and then called NumIterations with time tracking</note>
/// <remarks>Use 'nameof(DecoratedMethodName)' to request the application of method and iteration lifecycle methods</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class SailfishMethodAttribute : Attribute
{
    // TODO: Enable / disable by method
}