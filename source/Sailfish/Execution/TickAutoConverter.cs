using System.Diagnostics;

namespace Sailfish.Execution;

public static class TickAutoConverter
{
    private static long _systemFrequency = Stopwatch.Frequency;

    private static DurationConversion ConvertToNanoseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000_000;
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.Ns);
    }

    private static DurationConversion ConvertToMicroseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000;
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.Us);
    }

    private static DurationConversion ConvertToMilliseconds(long elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000;
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.Ms);
    }

    private static DurationConversion ConvertToSeconds(long elapsedTicks)
    {
        var result = (double)elapsedTicks / (double)Stopwatch.Frequency;
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.S);
    }

    private static DurationConversion ConvertToSeconds(double elapsedTicks)
    {
        var result = (double)elapsedTicks / (double)Stopwatch.Frequency;
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.S);
    }

    private static DurationConversion ConvertToMinutes(long elapsedTicks)
    {
        var result = (long)(ConvertToSeconds(elapsedTicks).Duration / 60.0);
        return new DurationConversion(result, DurationConversion.TimeScaleUnit.M);
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

    public static double ToMilliseconds(this double elapsedTicks)
    {
        return ConvertToSeconds(elapsedTicks).Duration * 1_000;
    }
}