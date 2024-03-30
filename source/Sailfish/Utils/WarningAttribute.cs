using System;

namespace Sailfish.Utils;

[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Event
    | AttributeTargets.Delegate,
    Inherited = false)]
internal sealed class WarningAttribute(string? message) : Attribute
{
    public string? Message { get; } = message;
}