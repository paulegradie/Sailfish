namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishObservation
{
    public ScaleFishObservation(string methodName, string propertyName, ComplexityMeasurement[] complexityMeasurements)
    {
        MethodName = methodName;
        PropertyName = propertyName;
        ComplexityMeasurements = complexityMeasurements;
    }

    public string MethodName { get; init; }
    public string PropertyName { get; init; }
    public ComplexityMeasurement[] ComplexityMeasurements { get; init; }

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