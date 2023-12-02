namespace Sailfish.Logging;

internal static class Log
{
    public static ILogger Logger { get; set; } = new SilentLogger();
}