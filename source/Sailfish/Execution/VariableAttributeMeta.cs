namespace Sailfish.Execution;

internal class VariableAttributeMeta
{
    public VariableAttributeMeta(object[] orderedVariables, bool estimateComplexity)
    {
        OrderedVariables = orderedVariables;
        EstimateComplexity = estimateComplexity;
    }

    public object[] OrderedVariables { get; }
    public bool EstimateComplexity { get; }
}