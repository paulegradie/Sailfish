namespace Sailfish.Analysis.ScaleFish;

public class ComplexityMeasurement(double x, double y)
{
    /// <summary>
    ///     An integer variable that represents a scale for some aspect of your system. 1 record in the database, 10 elements
    ///     in a thing
    /// </summary>
    public double X { get; set; } = x;

    /// <summary>
    ///     A double that represents the resulting time measurement for the given X
    /// </summary>
    public double Y { get; set; } = y;
}