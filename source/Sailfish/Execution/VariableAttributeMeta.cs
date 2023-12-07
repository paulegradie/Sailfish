namespace Sailfish.Execution;

internal class VariableAttributeMeta(object[] orderedVariables, bool estimateComplexity)
{
    public object[] OrderedVariables { get; } = orderedVariables;
    public bool EstimateComplexity { get; } = estimateComplexity;
}