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
    public WarningAttribute()
    {
    }

    public WarningAttribute(string? message)
    {
        Message = message;
    }

    public WarningAttribute(string? message, bool error)
    {
        Message = message;
        IsError = error;
    }

    public string? Message { get; }

    public bool IsError { get; }

    public string? DiagnosticId { get; set; }

    public string? UrlFormat { get; set; }
}