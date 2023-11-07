using System.Collections.Generic;
using System.Reflection;

internal record ComplexityCase(string ComplexityPropertyName, PropertyInfo ComplexityProperty, int VariableCount, List<int> Variables);