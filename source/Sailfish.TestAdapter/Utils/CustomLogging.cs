using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sailfish.TestAdapter.Utils;

internal static class CustomLogger
{
    public static string? FilePath;


    public static void VerbosePadded(string messageLine, params string[] properties)
    {
        FilePath ??= FilePath ?? $"C:\\Users\\paule\\code\\ProjectSailfish\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";

        if (!File.Exists(FilePath)) File.Create(FilePath);

        try
        {
            DoWrite(FilePath, "\r" + messageLine + "\r", properties);
        }
        catch (Exception ex)
        {
            FilePath = $"C:\\Users\\paule\\code\\ProjectSailfish\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";
            DoWrite(FilePath, $"What a crazy exception! How is it possible that: {ex.Message}");
            DoWrite(FilePath, "\r" + messageLine + "\r", properties);
        }
    }

    public static void Verbose(string messageLine, params string[] properties)
    {
        if (FilePath is null) FilePath = FilePath ?? $"C:\\Users\\paule\\code\\ProjectSailfish\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";

        if (!File.Exists(FilePath)) File.Create(FilePath);

        try
        {
            DoWrite(FilePath, messageLine, properties);
        }
        catch (Exception ex)
        {
            FilePath = $"C:\\Users\\paule\\code\\ProjectSailfish\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";
            DoWrite(FilePath, $"What a crazy exception!: {ex.Message}", properties);
            DoWrite(FilePath, messageLine, properties);
        }
    }

    private static void DoWrite(string fp, string messageLine, params string[] properties)
    {
        using var mutex = new Mutex(false, "THE_ONLY_Sail_MUTEX");
        mutex.WaitOne();

        using var writer = new StreamWriter(fp, true);
        var regex = new Regex("{(.+?)}");
        var matches = regex
            .Matches(messageLine)
            .Select(x => x.ToString())
            .ToArray();

        var pairs = matches.Zip(properties).ToArray();

        foreach (var (original, replacement) in pairs) messageLine = messageLine.Replace(original, replacement);

        writer.WriteLine(" - " + messageLine);
        writer.Flush();

        mutex.ReleaseMutex();
    }
}