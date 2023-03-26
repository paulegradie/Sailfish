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
        var supressConsole = type.GetCustomAttribute<SuppressConsoleAttribute>();

        var numIterations = type.GetNumIterations();
        var numWarmupIterations = type.GetWarmupIterations();

        return new ExecutionSettings
        {
            AsCsv = asCsv is not null,
            AsConsole = supressConsole is null,
            AsMarkdown = asMarkdown is not null,
            NumIterations = numIterations,
            NumWarmupIterations = numWarmupIterations
        };
    }
}