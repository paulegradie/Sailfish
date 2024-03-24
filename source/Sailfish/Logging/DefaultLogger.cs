using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sailfish.Logging;

internal class DefaultLogger : ILogger
{
    private readonly IEnumerable<LogLevel> allowedLogLevels;

    private readonly Dictionary<LogLevel, ConsoleColor> levelColors = new()
    {
        { LogLevel.Verbose, ConsoleColor.Gray }, // Gray for verbose, as it's usually less important
        { LogLevel.Debug, ConsoleColor.Blue }, // Blue for debug, a standard color for debugging information
        { LogLevel.Information, ConsoleColor.White }, // White for general information
        { LogLevel.Warning, ConsoleColor.Yellow }, // Yellow for warnings, as it's often associated with caution
        { LogLevel.Error, ConsoleColor.Red }, // Red for errors, indicating danger or serious issues
        { LogLevel.Fatal, ConsoleColor.DarkRed } // Dark red for fatal errors, indicating critical problems
    };

    private readonly Dictionary<LogLevel, string> nameMap = new()
    {
        { LogLevel.Verbose, "VRB" },
        { LogLevel.Debug, "DBG" },
        { LogLevel.Information, "INF" },
        { LogLevel.Warning, "WRN" },
        { LogLevel.Error, "ERR" },
        { LogLevel.Fatal, "FATAL" }
    };

    public DefaultLogger(LogLevel minimumLogLevel)
    {
        allowedLogLevels = new List<LogLevel>
            {
                LogLevel.Verbose,
                LogLevel.Debug,
                LogLevel.Information,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Fatal
            }
            .SkipWhile(x => x != minimumLogLevel);
    }

    public void Log(LogLevel level, string template, params object[] values)
    {
        var formattedLog = FormatTemplate(template, values);
        JoinAndWriteLines(level, new[] { formattedLog });
    }

    public void Log(LogLevel level, Exception ex, string template, params object[] values)
    {
        var lines = new List<string> { template, ex.Message };
        if (ex.StackTrace is not null)
            lines.Add(ex.StackTrace);

        if (ex.InnerException is not null)
        {
            var innerExceptionMessage = ex.InnerException?.Message;
            if (innerExceptionMessage is not null) lines.Add(innerExceptionMessage);

            var innerStackTrace = ex.InnerException?.StackTrace;
            if (innerStackTrace is not null) lines.Add(innerStackTrace);
        }

        JoinAndWriteLines(level, lines);
    }

    private void JoinAndWriteLines(LogLevel level, IEnumerable<string> lines)
    {
        if (!allowedLogLevels.Contains(level)) return;
        foreach (var line in lines)
        {
            var timestamp = $"[{DateTime.Now:HH:mm:ss}";
            Console.Write(timestamp);
            Console.ForegroundColor = levelColors[level];
            Console.Write($" {nameMap[level]}");
            Console.WriteLine($"]: {line}");
            Console.ResetColor();
        }
    }

    private static string FormatTemplate(string template, params object[] values)
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