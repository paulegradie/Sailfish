using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;

namespace VeerPerforma.Utils;

public class CustomLoggerOKAY
{
    private readonly string filePath;
    private Regex Regex = new("{(.+?)}");

    private object obj = new();

    public CustomLoggerOKAY(string filePath)
    {
        this.filePath = filePath;
    }

    public void Verbose(string messageLine, params string[] properties)
    {
        using (var mutex = new Mutex(false, "THE_ONLY_VEER_MUTEX"))
        {
            mutex.WaitOne();

            using var writer = new StreamWriter(filePath, append: true);

            var matches = Regex
                .Matches(messageLine)
                .Select(x => x.ToString())
                .ToArray();

            var pairs = matches.Zip(properties).ToArray();

            foreach (var (original, replacement) in pairs)
            {
                messageLine = messageLine.Replace(original, replacement);
            }

            writer.WriteLine(messageLine);
            writer.Flush();

            mutex.ReleaseMutex();
        }
    }

    public static CustomLoggerOKAY CreateLogger(string filePath)
    {
        if (!File.Exists(filePath))
        {
            File.Create(filePath);
        }

        return new CustomLoggerOKAY(filePath);
    }
}