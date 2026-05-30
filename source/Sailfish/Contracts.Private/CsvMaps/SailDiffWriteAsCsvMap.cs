using CsvHelper.Configuration;
using Sailfish.Contracts.Private.CsvMaps.Converters;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Private.CsvMaps;

internal sealed class SailDiffWriteAsCsvMap : ClassMap<SailDiffResult>
{
    public SailDiffWriteAsCsvMap()
    {
        // Legacy columns 0..11 preserved verbatim for back-compat with existing CSV
        // consumers. Tier-2/3 magnitude columns appended from index 12 onwards via Convert
        // — CsvHelper can't extract nested-record / nullable fields from a member-access
        // expression alone, so we project to strings explicitly.
        Map(m => m.TestCaseId.DisplayName).Index(0);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore).Index(1);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter).Index(2);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore).Index(3);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter).Index(4);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.TestStatistic).Index(5);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue).Index(6);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription).Index(7);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore).Index(8);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter).Index(9);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.RawDataBefore).TypeConverter<DoubleArrayCsvConverter>().Index(10);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.RawDataAfter).TypeConverter<DoubleArrayCsvConverter>().Index(11);

        // BH-FDR–adjusted q-value across the family of comparisons.
        Map().Name("QValue").Index(12).Convert(args => Stat(args.Value)?.QValue?.ToString() ?? string.Empty);

        // Effect size: name + value + CI bounds. Empty cells when the test type didn't
        // emit an effect size (e.g. KS).
        Map().Name("EffectName").Index(13).Convert(args => Stat(args.Value)?.EffectSize?.Name ?? string.Empty);
        Map().Name("EffectValue").Index(14).Convert(args => Stat(args.Value)?.EffectSize?.Value.ToString() ?? string.Empty);
        Map().Name("EffectCiLower").Index(15).Convert(args => Stat(args.Value)?.EffectSize?.CiLower?.ToString() ?? string.Empty);
        Map().Name("EffectCiUpper").Index(16).Convert(args => Stat(args.Value)?.EffectSize?.CiUpper?.ToString() ?? string.Empty);

        // Shift estimate (Mean diff / Hodges-Lehmann / Log-ratio) — name, value, CI, units.
        Map().Name("ShiftName").Index(17).Convert(args => Stat(args.Value)?.Difference?.Name ?? string.Empty);
        Map().Name("ShiftValue").Index(18).Convert(args => Stat(args.Value)?.Difference?.Value.ToString() ?? string.Empty);
        Map().Name("ShiftCiLower").Index(19).Convert(args => Stat(args.Value)?.Difference?.CiLower?.ToString() ?? string.Empty);
        Map().Name("ShiftCiUpper").Index(20).Convert(args => Stat(args.Value)?.Difference?.CiUpper?.ToString() ?? string.Empty);
        Map().Name("ShiftUnits").Index(21).Convert(args => Stat(args.Value)?.Difference?.Units ?? string.Empty);

        // Minimum detectable effect as a percentage of the pooled mean.
        Map().Name("MdePercent").Index(22).Convert(args =>
            Stat(args.Value)?.MinimumDetectableEffectPercent?.ToString() ?? string.Empty);
    }

    private static StatisticalTestResult? Stat(object value) =>
        (value as SailDiffResult)?.TestResultsWithOutlierAnalysis?.StatisticalTestResult;
}
