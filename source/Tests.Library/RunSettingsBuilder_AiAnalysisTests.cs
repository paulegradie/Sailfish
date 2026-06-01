using Sailfish;
using Sailfish.Analysis.Ai;
using Shouldly;
using Xunit;

namespace Tests.Library;

public class RunSettingsBuilder_AiAnalysisTests
{
    [Fact]
    public void AiAnalysis_IsOptIn_DefaultsOff()
    {
        var settings = RunSettingsBuilder.CreateBuilder().Build();

        settings.RunAiAnalysis.ShouldBeFalse();
        settings.AiAnalysisSettings.ShouldNotBeNull();
        settings.AiAnalysisSettings.Role.ShouldBe(SkipperRole.Explain);
    }

    [Fact]
    public void WithAiAnalysis_EnablesIt_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithAiAnalysis();

        result.ShouldBeSameAs(builder);
        builder.Build().RunAiAnalysis.ShouldBeTrue();
    }

    [Fact]
    public void WithAiAnalysis_WithCustomSettings_FlowsThrough()
    {
        var custom = new AiAnalysisSettings(emitConsoleSummary: false, writeReviewArtifact: false, useResponseCache: false);

        var settings = RunSettingsBuilder.CreateBuilder().WithAiAnalysis(custom).Build();

        settings.RunAiAnalysis.ShouldBeTrue();
        settings.AiAnalysisSettings.ShouldBeSameAs(custom);
        settings.AiAnalysisSettings.EmitConsoleSummary.ShouldBeFalse();
        settings.AiAnalysisSettings.WriteReviewArtifact.ShouldBeFalse();
        settings.AiAnalysisSettings.UseResponseCache.ShouldBeFalse();
    }
}
