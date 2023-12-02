using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Analysis.ScaleFish;

internal record ComplexityCase(string ComplexityPropertyName, PropertyInfo ComplexityProperty, int VariableCount, List<int> Variables);