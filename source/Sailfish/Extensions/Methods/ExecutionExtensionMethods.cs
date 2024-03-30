using System;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Execution;

namespace Sailfish.Extensions.Methods;

internal static class ExecutionExtensionMethods
{
    public static IExecutionSettings RetrieveExecutionTestSettings(this Type type, int? globalSampleSize, int? globalNumWarmupIterations)
    {
        var asMarkdown = type.GetCustomAttribute<WriteToMarkdownAttribute>();
        var asCsv = type.GetCustomAttribute<WriteToCsvAttribute>();
        var suppressConsole = type.GetCustomAttribute<SuppressConsoleAttribute>();

        var sampleSize = globalSampleSize ?? type.GetSampleSize();
        var numWarmupIterations = globalNumWarmupIterations ?? type.GetWarmupIterations();

        return new ExecutionSettings(asCsv is not null, suppressConsole is null, asMarkdown is not null, sampleSize,
            numWarmupIterations);
    }
}