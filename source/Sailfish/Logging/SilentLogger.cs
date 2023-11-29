using System;

namespace Sailfish.Logging;

internal class SilentLogger : ILogger
{
    public void Verbose(string message)
    {
    }

    public void Verbose(string template, params object[] values)
    {
    }

    public void Verbose(Exception ex, string template)
    {
    }

    public void Verbose(Exception ex, string template, params object[] values)
    {
    }

    public void Debug(string message)
    {
    }

    public void Debug(string template, params object[] values)
    {
    }

    public void Debug(Exception ex, string template)
    {
    }

    public void Debug(Exception ex, string template, params object[] values)
    {
    }

    public void Information(string message)
    {
    }

    public void Information(string template, params object[] values)
    {
    }

    public void Information(Exception ex, string template)
    {
    }

    public void Information(Exception ex, string template, params object[] values)
    {
    }

    public void Warning(string message)
    {
    }

    public void Warning(string template, params object[] values)
    {
    }

    public void Warning(Exception ex, string template)
    {
    }

    public void Warning(Exception ex, string template, params object[] values)
    {
    }

    public void Error(string message)
    {
    }

    public void Error(string template, params object[] values)
    {
    }

    public void Error(Exception ex, string template)
    {
    }

    public void Error(Exception ex, string template, params object[] values)
    {
    }

    public void Fatal(string message)
    {
    }

    public void Fatal(string template, params object[] values)
    {
    }

    public void Fatal(Exception ex, string template)
    {
    }

    public void Fatal(Exception ex, string template, params object[] values)
    {
    }
}