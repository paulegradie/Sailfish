using System;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Statistics;

public class CompiledResult
{
    public CompiledResult(string displayName, TestCaseStatistics testCaseStatistics)
    {
        DisplayName = displayName;
        TestCaseStatistics = testCaseStatistics;
    }

    public TestCaseStatistics TestCaseStatistics { get; set; }
    public Exception? Exception { get; set; }
    public string DisplayName { get; set; }
}

public static class ExecutionSettingsExtensionMethods
{
    public static ExecutionSettings GetExecutionSettings(this Type type)
    {
        var settings = new ExecutionSettings();
        if (type.HasAttribute<VeerPerformaAttribute>())
        {
            // TODO: derive settings from the perf type.
            // These will come from an attribute Search.
            settings.AsConsole = true;
            settings.AsCsv = false;
            return settings;
        }
        else
        {
            return settings;
        }
    }
}