using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Sailfish.Logging;

internal class DefaultLogger : ILogger
{
    public void Verbose(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Verbose(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Verbose(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Verbose(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    public void Debug(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Debug(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Debug(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Debug(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    public void Information(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Information(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Information(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Information(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    public void Warning(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Warning(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Warning(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Warning(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    public void Error(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Error(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Error(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Error(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    public void Fatal(string message)
    {
        JoinAndWriteLines(message);
    }

    public void Fatal(string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values));
    }

    public void Fatal(Exception ex, string message)
    {
        JoinAndWriteLines(message, UnpackException(ex));
    }

    public void Fatal(Exception ex, string template, params object[] values)
    {
        JoinAndWriteLines(WriteTemplate(template, values), UnpackException(ex));
    }

    private static void JoinAndWriteLines(params string[] lines)
    {
        var linesToWrite = new StringBuilder();
        foreach (var line in lines)
        {
            linesToWrite.AppendLine(line);
        }

        Console.WriteLine(linesToWrite.ToString());
    }

    public string UnpackException(Exception exception, [CallerMemberName] string? memberName = null)
    {
        var innerLineBuilder = new StringBuilder();
        foreach (var innerLine in exception.InnerException?.Message.Split() ?? Array.Empty<string>())
        {
            innerLineBuilder.AppendLine($"{nameMap[memberName!]} {innerLine}");
        }

        return new StringBuilder()
            .AppendLine(exception.Message)
            .AppendLine(innerLineBuilder.ToString())
            .ToString();
    }

    private readonly Dictionary<string, string> nameMap = new()
    {
        { nameof(Verbose), "[Verb]:" },
        { nameof(Debug), "[Debug]:" },
        { nameof(Information), "[Inf]:" },
        { nameof(Warning), "[Warn]:" },
        { nameof(Error), "[Err]:" },
        { nameof(Fatal), "[Fatal]:" },
    };

    private static string WriteTemplate(string template, params object[] values)
    {
        var populatedTemplate = (string)template.Clone();
        var matches = new Regex("{(.+?)}")
            .Matches(template)
            .Select(x => x.ToString())
            .ToArray();

        var pairs = matches.Zip(values).ToArray();

        foreach (var (original, replacement) in pairs) populatedTemplate = populatedTemplate.Replace(original, replacement.ToString());
        return populatedTemplate;
    }
}