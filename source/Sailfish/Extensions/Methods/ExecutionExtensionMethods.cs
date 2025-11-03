using System;
using System.Linq;
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
        var sailfishAttribute = type.GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single();

        var sampleSize = globalSampleSize ?? type.GetSampleSize();
        var numWarmupIterations = globalNumWarmupIterations ?? type.GetWarmupIterations();

        return new ExecutionSettings(asCsv is not null, suppressConsole is null, asMarkdown is not null, sampleSize,
            numWarmupIterations)
        {
            DisableOverheadEstimation = sailfishAttribute.DisableOverheadEstimation,

            // NEW: Adaptive sampling settings
            UseAdaptiveSampling = sailfishAttribute.UseAdaptiveSampling,
            TargetCoefficientOfVariation = sailfishAttribute.TargetCoefficientOfVariation,
            MaximumSampleSize = sailfishAttribute.MaximumSampleSize,
            MinimumSampleSize = 10, // Default minimum
            ConfidenceLevel = 0.95 // Default confidence level
        };
    }

    public static IExecutionSettings RetrieveExecutionTestSettings(
        this Type type,
        int? globalSampleSize,
        int? globalNumWarmupIterations,
        bool? globalUseAdaptiveSampling,
        double? globalTargetCoefficientOfVariation,
        int? globalMaximumSampleSize)
    {
        var settings = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);
        if (globalUseAdaptiveSampling.HasValue)
        {
            settings.UseAdaptiveSampling = globalUseAdaptiveSampling.Value;
        }

        if (globalTargetCoefficientOfVariation.HasValue)
        {
            settings.TargetCoefficientOfVariation = globalTargetCoefficientOfVariation.Value;
        }

        if (globalMaximumSampleSize.HasValue)
        {
            settings.MaximumSampleSize = globalMaximumSampleSize.Value;
        }

        return settings;
    }

}