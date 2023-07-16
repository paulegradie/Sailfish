namespace Sailfish.Execution;

public class DurationConversion
{
    public DurationConversion(double duration, TimeScaleUnit timeScaleUnit)
    {
        Duration = duration;
        TimeScale = timeScaleUnit;
    }

    public double Duration { get; set; }
    public TimeScaleUnit TimeScale { get; set; }

    public enum TimeScaleUnit
    {
        Ns,
        Us,
        Ms,
        S,
        M
    }
}