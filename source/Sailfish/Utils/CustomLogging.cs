using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sailfish.Utils
{
    public static class logger
    {
        public static string? filePath;


        public static void VerbosePadded(string messageLine, params string[] properties)
        {
            if (filePath is null) filePath = filePath ?? $"C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";

            if (!File.Exists(filePath)) File.Create(filePath);

            try
            {
                DoWrite(filePath, "\r" + messageLine + "\r", properties);
            }
            catch (Exception ex)
            {
                filePath = $"C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";
                DoWrite(filePath, $"What a crazy exception! How is it possible that: {ex.Message}");
                DoWrite(filePath, "\r" + messageLine + "\r", properties);
            }
        }

        public static void Verbose(string messageLine, params string[] properties)
        {
            if (filePath is null) filePath = filePath ?? $"C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";

            if (!File.Exists(filePath)) File.Create(filePath);

            try
            {
                DoWrite(filePath, messageLine, properties);
            }
            catch (Exception ex)
            {
                filePath = $"C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs\\crazy_logs-{Guid.NewGuid().ToString()}.txt";
                DoWrite(filePath, $"What a crazy exception!: {ex.Message}", properties);
                DoWrite(filePath, messageLine, properties);
            }
        }

        private static void DoWrite(string fp, string messageLine, params string[] properties)
        {
            using (var mutex = new Mutex(false, "THE_ONLY_VEER_MUTEX"))
            {
                mutex.WaitOne();

                using var writer = new StreamWriter(fp, append: true);
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
    }
}