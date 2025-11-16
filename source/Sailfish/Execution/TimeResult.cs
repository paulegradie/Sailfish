namespace Sailfish.Execution;

public class TimeResult
{
    public TimeResult(DurationConversion nanoSeconds,
        DurationConversion microSeconds,
        DurationConversion milliSeconds,
        DurationConversion seconds,
        DurationConversion minutes)
    {
        NanoSeconds = nanoSeconds;
        MicroSeconds = microSeconds;
        MilliSeconds = milliSeconds;
        Seconds = seconds;
        Minutes = minutes;
    }

    public DurationConversion NanoSeconds { get; set; }
    public DurationConversion MicroSeconds { get; set; }
    public DurationConversion MilliSeconds { get; set; }
    public DurationConversion Seconds { get; set; }
    public DurationConversion Minutes { get; set; }
}