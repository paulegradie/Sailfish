namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishObservation(string methodName, string propertyName, ComplexityMeasurement[] complexityMeasurements)
{
    public string MethodName { get; init; } = methodName;
    public string PropertyName { get; init; } = propertyName;
    public ComplexityMeasurement[] ComplexityMeasurements { get; init; } = complexityMeasurements;

    public void Deconstruct(out string methodName, out string propertyName, out ComplexityMeasurement[] complexityMeasurements)
    {
        methodName = MethodName;
        propertyName = PropertyName;
        complexityMeasurements = ComplexityMeasurements;
    }

    public override string ToString()
    {
        return string.Join(".", MethodName, PropertyName);
    }
}