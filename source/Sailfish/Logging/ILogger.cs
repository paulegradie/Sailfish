using System;

namespace Sailfish.Logging;

public interface ILogger
{
    void Verbose(string message);
    void Verbose(string template, params object[] values);
    void Verbose(Exception ex, string template);
    void Verbose(Exception ex, string template, params object[] values);

    void Debug(string message);
    void Debug(string template, params object[] values);
    void Debug(Exception ex, string template);
    void Debug(Exception ex, string template, params object[] values);

    void Information(string message);
    void Information(string template, params object[] values);
    void Information(Exception ex, string template);
    void Information(Exception ex, string template, params object[] values);

    void Warning(string message);
    void Warning(string template, params object[] values);
    void Warning(Exception ex, string template);
    void Warning(Exception ex, string template, params object[] values);

    void Error(string message);
    void Error(string template, params object[] values);
    void Error(Exception ex, string template);
    void Error(Exception ex, string template, params object[] values);

    void Fatal(string message);
    void Fatal(string template, params object[] values);
    void Fatal(Exception ex, string template);
    void Fatal(Exception ex, string template, params object[] values);
}