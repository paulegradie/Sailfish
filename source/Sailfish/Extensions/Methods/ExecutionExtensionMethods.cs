using System;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Execution;

namespace Sailfish.Extensions.Methods;

internal static class ExecutionExtensionMethods
{
    public static IExecutionSettings RetrieveExecutionTestSettings(this Type type)
    {
        var asMarkdown = type.GetCustomAttribute<WriteToMarkdownAttribute>();
        var asCsv = type.GetCustomAttribute<WriteToCsvAttribute>();
        var suppressConsole = type.GetCustomAttribute<SuppressConsoleAttribute>();

        var sampleSize = type.GetSampleSize();
        var numWarmupIterations = type.GetWarmupIterations();

        return new ExecutionSettings
        {
            AsCsv = asCsv is not null,
            AsConsole = suppressConsole is null,
            AsMarkdown = asMarkdown is not null,
            SampleSize = sampleSize,
            NumWarmupIterations = numWarmupIterations
        };
    }
}