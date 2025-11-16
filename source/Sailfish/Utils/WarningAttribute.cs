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
internal sealed class WarningAttribute : Attribute
{
    public WarningAttribute(string? message)
    {
        Message = message;
    }

    public string? Message { get; }
}