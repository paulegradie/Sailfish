using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Analysis.ScaleFish;

internal record ComplexityCase
{
    public ComplexityCase(string ComplexityPropertyName,
        PropertyInfo ComplexityProperty,
        int VariableCount,
        List<int> Variables)
    {
        this.ComplexityPropertyName = ComplexityPropertyName;
        this.ComplexityProperty = ComplexityProperty;
        this.VariableCount = VariableCount;
        this.Variables = Variables;
    }

    public string ComplexityPropertyName { get; init; }
    public PropertyInfo ComplexityProperty { get; init; }
    public int VariableCount { get; init; }
    public List<int> Variables { get; init; }

    public void Deconstruct(out string ComplexityPropertyName, out PropertyInfo ComplexityProperty, out int VariableCount, out List<int> Variables)
    {
        ComplexityPropertyName = this.ComplexityPropertyName;
        ComplexityProperty = this.ComplexityProperty;
        VariableCount = this.VariableCount;
        Variables = this.Variables;
    }
}