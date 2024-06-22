using System.Diagnostics;

namespace Sailfish.Execution;

public static class TickAutoConverter
{
    private static DurationConversion ConvertToNanoseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToMicroseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToMilliseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToSeconds(long elapsedTicks)
    {
        var result = elapsedTicks / (double)Stopwatch.Frequency;
        return new DurationConversion(result);
    }


    private static DurationConversion ConvertToMinutes(long elapsedTicks)
    {
        var result = (long)(ConvertToSeconds(elapsedTicks).Duration / 60.0);
        return new DurationConversion(result);
    }

    public static TimeResult ConvertToTime(long elapsedTicks)
    {
        return new TimeResult(
            ConvertToNanoseconds(elapsedTicks),
            ConvertToMicroseconds(elapsedTicks),
            ConvertToMilliseconds(elapsedTicks),
            ConvertToSeconds(elapsedTicks),
            ConvertToMinutes(elapsedTicks)
        );
    }
}