using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class ExecutionSettingsTrackingFormatBuilder
{
    private bool? _asConsole;
    private bool? _asCsv;
    private bool? _asMarkdown;
    private bool? _disableOverheadEstimation;
    private int? _numWarmupIterations;
    private int? _sampleSize;

    public static ExecutionSettingsTrackingFormatBuilder Create()
    {
        return new ExecutionSettingsTrackingFormatBuilder();
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsCsv(bool asCsv)
    {
        this._asCsv = asCsv;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsConsole(bool asConsole)
    {
        this._asConsole = asConsole;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsMarkdown(bool asMarkdown)
    {
        this._asMarkdown = asMarkdown;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        this._numWarmupIterations = numWarmupIterations;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithSampleSize(int sampleSize)
    {
        this._sampleSize = sampleSize;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithDisableOverheadEstimation(bool disableOverheadEstimation)
    {
        this._disableOverheadEstimation = disableOverheadEstimation;
        return this;
    }

    public ExecutionSettingsTrackingFormat Build()
    {
        return new ExecutionSettingsTrackingFormat(
            _asCsv ?? false,
            _asConsole ?? false,
            _asMarkdown ?? false,
            _numWarmupIterations ?? 1,
            _sampleSize ?? 3,
            _disableOverheadEstimation ?? false
        );
    }
}