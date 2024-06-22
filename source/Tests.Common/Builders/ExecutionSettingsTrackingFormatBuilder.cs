using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class ExecutionSettingsTrackingFormatBuilder
{
    private bool? asConsole;
    private bool? asCsv;
    private bool? asMarkdown;
    private bool? disableOverheadEstimation;
    private int? numWarmupIterations;
    private int? sampleSize;

    public static ExecutionSettingsTrackingFormatBuilder Create()
    {
        return new ExecutionSettingsTrackingFormatBuilder();
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsCsv(bool asCsv)
    {
        this.asCsv = asCsv;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsConsole(bool asConsole)
    {
        this.asConsole = asConsole;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithAsMarkdown(bool asMarkdown)
    {
        this.asMarkdown = asMarkdown;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        this.numWarmupIterations = numWarmupIterations;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithSampleSize(int sampleSize)
    {
        this.sampleSize = sampleSize;
        return this;
    }

    public ExecutionSettingsTrackingFormatBuilder WithDisableOverheadEstimation(bool disableOverheadEstimation)
    {
        this.disableOverheadEstimation = disableOverheadEstimation;
        return this;
    }

    public ExecutionSettingsTrackingFormat Build()
    {
        return new ExecutionSettingsTrackingFormat(
            asCsv ?? false,
            asConsole ?? false,
            asMarkdown ?? false,
            numWarmupIterations ?? 1,
            sampleSize ?? 3,
            disableOverheadEstimation ?? false
        );
    }
}