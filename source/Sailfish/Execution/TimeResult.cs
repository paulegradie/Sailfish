namespace Sailfish.Execution;

public class TimeResult(
    DurationConversion nanoSeconds,
    DurationConversion microSeconds,
    DurationConversion milliSeconds,
    DurationConversion seconds,
    DurationConversion minutes)
{
    public DurationConversion NanoSeconds { get; set; } = nanoSeconds;
    public DurationConversion MicroSeconds { get; set; } = microSeconds;
    public DurationConversion MilliSeconds { get; set; } = milliSeconds;
    public DurationConversion Seconds { get; set; } = seconds;
    public DurationConversion Minutes { get; set; } = minutes;
}