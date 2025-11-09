using System;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Analysis;

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

            // NEW: Adaptive sampling and statistics
            UseAdaptiveSampling = sailfishAttribute.UseAdaptiveSampling,
            TargetCoefficientOfVariation = sailfishAttribute.TargetCoefficientOfVariation,
            MaximumSampleSize = sailfishAttribute.MaximumSampleSize,
            MinimumSampleSize = sailfishAttribute.MinimumSampleSize,
            ConfidenceLevel = sailfishAttribute.ConfidenceLevel,
            MaxConfidenceIntervalWidth = sailfishAttribute.MaxConfidenceIntervalWidth,
            UseRelativeConfidenceInterval = sailfishAttribute.UseRelativeConfidenceInterval,

            // NEW: Outlier handling
            OutlierStrategy = sailfishAttribute.OutlierStrategy,
            UseConfigurableOutlierDetection = sailfishAttribute.UseConfigurableOutlierDetection,


            // NEW: Execution tuning and diagnostics
            OperationsPerInvoke = sailfishAttribute.OperationsPerInvoke,
            TargetIterationDuration = TimeSpan.FromMilliseconds(sailfishAttribute.TargetIterationDurationMs),
            MaxMeasurementTimePerMethod = sailfishAttribute.MaxMeasurementTimePerMethodMs > 0 ? TimeSpan.FromMilliseconds(sailfishAttribute.MaxMeasurementTimePerMethodMs) : (TimeSpan?)null,
            EnableDefaultDiagnosers = sailfishAttribute.EnableDefaultDiagnosers,
            UseTimeBudgetController = sailfishAttribute.UseTimeBudgetController
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


        public static IExecutionSettings RetrieveExecutionTestSettings(
            this Type type,
            int? globalSampleSize,
            int? globalNumWarmupIterations,
            bool? globalUseAdaptiveSampling,
            double? globalTargetCoefficientOfVariation,
            int? globalMaximumSampleSize,
            bool? globalUseConfigurableOutlierDetection,
            OutlierStrategy? globalOutlierStrategy)
        {
            var settings = type.RetrieveExecutionTestSettings(
                globalSampleSize,
                globalNumWarmupIterations,
                globalUseAdaptiveSampling,
                globalTargetCoefficientOfVariation,
                globalMaximumSampleSize);

            if (globalUseConfigurableOutlierDetection.HasValue)
            {
                settings.UseConfigurableOutlierDetection = globalUseConfigurableOutlierDetection.Value;
            }

            if (globalOutlierStrategy.HasValue)
            {
                settings.OutlierStrategy = globalOutlierStrategy.Value;
            }

            return settings;
        }

}