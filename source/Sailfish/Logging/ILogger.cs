using System;

namespace Sailfish.Logging;

public interface ILogger
{
    void Log(LogLevel level, string template, params object[] values);

    void Log(LogLevel level, Exception ex, string template, params object[] values);
}