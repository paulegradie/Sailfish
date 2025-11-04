using System;

namespace Sailfish.Contracts.Public.Models;

public record ValidationWarning(string Code, string Message, ValidationSeverity Severity, string? Details = null)
{
    public override string ToString()
    {
        return $"[{Severity}] {Code}: {Message}{(string.IsNullOrWhiteSpace(Details) ? string.Empty : " (" + Details + ")")}";
    }
}

