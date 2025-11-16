namespace Sailfish.Contracts.Public.Models;

public record ValidationWarning
{
    public ValidationWarning(string Code, string Message, ValidationSeverity Severity, string? Details = null)
    {
        this.Code = Code;
        this.Message = Message;
        this.Severity = Severity;
        this.Details = Details;
    }

    public override string ToString()
    {
        return $"[{Severity}] {Code}: {Message}{(string.IsNullOrWhiteSpace(Details) ? string.Empty : " (" + Details + ")")}";
    }

    public string Code { get; init; }
    public string Message { get; init; }
    public ValidationSeverity Severity { get; init; }
    public string? Details { get; init; }

    public void Deconstruct(out string Code, out string Message, out ValidationSeverity Severity, out string? Details)
    {
        Code = this.Code;
        Message = this.Message;
        Severity = this.Severity;
        Details = this.Details;
    }
}

