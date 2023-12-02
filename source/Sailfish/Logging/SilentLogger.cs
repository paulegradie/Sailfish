using System;

namespace Sailfish.Logging;

internal class SilentLogger : ILogger
{
    public void Log(LogLevel level, string template, params object[] values)
    {
    }

    public void Log(LogLevel level, Exception ex, string template, params object[] values)
    {
    }
}