using Sailfish.Contracts.Public.Models;

namespace Tests.Common.Builders;

public class TestCaseNameBuilder
{
    private string? name;
    private IReadOnlyList<string>? parts;

    public TestCaseNameBuilder WithName(string name)
    {
        this.name = name;
        this.parts = null;
        return this;
    }

    public TestCaseNameBuilder WithParts(IReadOnlyList<string> parts)
    {
        this.parts = parts;
        name = string.Join(".", parts);
        return this;
    }

    public TestCaseName Build()
    {
        if (name == null && parts == null)
        {
            throw new InvalidOperationException("Either Name or Parts must be provided.");
        }

        if (parts != null)
        {
            return new TestCaseName(parts);
        }

        return new TestCaseName(name!);
    }
}