using Sailfish.Contracts.Public.Models;

namespace Tests.Common.Builders;

public class TestCaseNameBuilder
{
    private string? _name;
    private IReadOnlyList<string>? _parts;

    public static TestCaseNameBuilder Create() => new();

    public TestCaseNameBuilder WithName(string name)
    {
        this._name = name;
        _parts = null;
        return this;
    }

    public TestCaseNameBuilder WithParts(IReadOnlyList<string> parts)
    {
        this._parts = parts;
        _name = string.Join(".", parts);
        return this;
    }

    public TestCaseName Build()
    {
        if (_name == null && _parts == null) throw new InvalidOperationException("Either Name or Parts must be provided.");

        if (_parts != null) return new TestCaseName(_parts);

        return new TestCaseName(_name!);
    }
}