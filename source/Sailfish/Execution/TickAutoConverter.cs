using System.Diagnostics;

namespace Sailfish.Execution;

public static class TickAutoConverter
{
    private static DurationConversion ConvertToNanoseconds(double elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToMicroseconds(double elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToMilliseconds(double elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration * 1_000;
        return new DurationConversion(result);
    }

    private static DurationConversion ConvertToSeconds(double elapsedTicks)
    {
        var result = elapsedTicks / Stopwatch.Frequency;
        return new DurationConversion(result);
    }


    private static DurationConversion ConvertToMinutes(double elapsedTicks)
    {
        var result = ConvertToSeconds(elapsedTicks).Duration / 60.0;
        return new DurationConversion(result);
    }

    public static TimeResult ConvertToTime(long elapsedTicks)
    {
        return ConvertToTime((double)elapsedTicks);
    }

    // Sub-tick precision is preserved: OperationsPerInvoke normalization (batch ticks / N) and
    // overhead subtraction operate in floating point, so a per-operation duration is never quantized
    // to a whole Stopwatch tick before it reaches the statistics layer.
    public static TimeResult ConvertToTime(double elapsedTicks)
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
