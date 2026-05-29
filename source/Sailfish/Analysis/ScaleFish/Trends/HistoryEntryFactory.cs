using System;

namespace Sailfish.Analysis.ScaleFish.Trends;

/// <summary>
/// Projects a fitted <see cref="ScaleFishModel"/> down to a <see cref="ComplexityHistoryEntry"/> snapshot
/// suitable for persisting and diffing.
/// </summary>
public static class HistoryEntryFactory
{
    public static ComplexityHistoryEntry Build(
        string testClassFullName,
        string methodName,
        string propertyName,
        ScaleFishModel model,
        string commitSha,
        DateTime timestampUtc)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        var bestFn = model.ScaleFishModelFunction;
        var scale = bestFn?.FunctionParameters?.Scale ?? double.NaN;
        var bias = bestFn?.FunctionParameters?.Bias ?? double.NaN;

        return new ComplexityHistoryEntry(
            testClassFullName: testClassFullName ?? string.Empty,
            methodName: methodName ?? string.Empty,
            propertyName: propertyName ?? string.Empty,
            commitSha: string.IsNullOrWhiteSpace(commitSha) ? "unknown" : commitSha,
            timestampUtc: timestampUtc,
            bestFamilyName: bestFn?.Name ?? string.Empty,
            bestFamilyOName: bestFn?.OName ?? string.Empty,
            bestScale: scale,
            bestBias: bias,
            bestRSquared: model.GoodnessOfFit,
            bestAicc: model.BestAicc,
            akaikeWeight: model.AkaikeWeight,
            isDistinguishable: model.IsDistinguishable,
            sampleSize: model.SampleSize,
            continuousExponentB: model.PowerLog?.B,
            continuousExponentC: model.PowerLog?.C,
            cvRankAgreement: model.CrossValidation?.RankAgreement,
            bootstrapSelectionAgreement: model.Bootstrap?.SelectionAgreement);
    }
}
