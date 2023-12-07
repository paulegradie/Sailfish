namespace Sailfish.Execution;

public class DurationConversion(double duration, DurationConversion.TimeScaleUnit timeScaleUnit)
{
    public enum TimeScaleUnit
    {
        Ns,
        Us,
        Ms,
        S,
        M
    }

    public double Duration { get; set; } = duration;
}