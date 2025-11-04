using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish.Analysis;

public interface IStatisticalValidator
{
    ValidationResult Validate(PerformanceRunResult result, IExecutionSettings settings);
}

public class StatisticalValidator : IStatisticalValidator
{
    public ValidationResult Validate(PerformanceRunResult pr, IExecutionSettings settings)
    {
        var warnings = new List<ValidationWarning>();

        // 1) Low sample size after outlier removal
        var n = pr.DataWithOutliersRemoved?.Length ?? 0;
        if (n > 0 && n < Math.Max(10, settings.MinimumSampleSize))
        {
            warnings.Add(new ValidationWarning(
                "LOW_SAMPLE_SIZE",
                $"Only {n} effective samples after outlier removal; estimates may be unstable.",
                ValidationSeverity.Warning,
                "Consider increasing MinimumSampleSize or MaximumSampleSize."));
        }

        // 2) Excessive outlier rate based on raw sample count
        var rawN = pr.RawExecutionResults?.Length ?? n;
        if (rawN > 0)
        {
            var outlierRate = (double)pr.TotalNumOutliers / rawN;
            if (outlierRate >= 0.30)
            {
                warnings.Add(new ValidationWarning(
                    "EXCESSIVE_OUTLIERS",
                    $"Outliers comprise {outlierRate:P0} of samples.",
                    ValidationSeverity.Critical,
                    "Investigate environment noise, warmups, or tighten outlier strategy."));
            }
            else if (outlierRate >= 0.15)
            {
                warnings.Add(new ValidationWarning(
                    "ELEVATED_OUTLIERS",
                    $"Outliers comprise {outlierRate:P0} of samples.",
                    ValidationSeverity.Warning,
                    "Potential skew/heavy tails; consider reviewing test stability."));
            }
        }

        // 3) High coefficient of variation (CV)
        if (pr.Mean > 0 && settings.TargetCoefficientOfVariation > 0)
        {
            var cv = pr.StdDev / pr.Mean;
            if (cv >= settings.TargetCoefficientOfVariation * 3.0)
            {
                warnings.Add(new ValidationWarning(
                    "HIGH_CV",
                    $"Observed CV {cv:F3} is much higher than target {settings.TargetCoefficientOfVariation:F3}.",
                    ValidationSeverity.Critical,
                    "High variability; increase sampling or reduce noise."));
            }
            else if (cv >= settings.TargetCoefficientOfVariation * 2.0)
            {
                warnings.Add(new ValidationWarning(
                    "ELEVATED_CV",
                    $"Observed CV {cv:F3} exceeds 2× target {settings.TargetCoefficientOfVariation:F3}.",
                    ValidationSeverity.Warning,
                    "Variance may be unstable; consider more samples."));
            }
        }

        // 4) Confidence interval width relative to mean
        if (pr.Mean > 0)
        {
            var relWidth = pr.ConfidenceIntervalWidth / pr.Mean;
            if (relWidth >= settings.MaxConfidenceIntervalWidth * 2.0)
            {
                warnings.Add(new ValidationWarning(
                    "WIDE_CI",
                    $"CI width {relWidth:P1} is far above budget {settings.MaxConfidenceIntervalWidth:P1}.",
                    ValidationSeverity.Critical,
                    "Results are imprecise; increase sampling or reduce variance."));
            }
            else if (relWidth > settings.MaxConfidenceIntervalWidth * 1.2)
            {
                warnings.Add(new ValidationWarning(
                    "ELEVATED_CI",
                    $"CI width {relWidth:P1} exceeds budget {settings.MaxConfidenceIntervalWidth:P1}.",
                    ValidationSeverity.Warning,
                    "Consider increasing MaximumSampleSize or tuning parameters."));
            }
        }

        return new ValidationResult(warnings);
    }
}

