using Sailfish;
using Sailfish.Trawl;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlSettingsWiringTests
{
    [Fact]
    public void RunSettings_Has_NonNull_Default_TrawlSettings()
    {
        var settings = RunSettingsBuilder.CreateBuilder().Build();

        settings.TrawlSettings.ShouldNotBeNull();
        settings.TrawlSettings.Disabled.ShouldBeFalse();
        settings.TrawlSettings.VirtualUsersOverride.ShouldBeNull();
        settings.TrawlSettings.MaxDurationSecondsOverride.ShouldBeNull();
    }

    [Fact]
    public void Builder_WithTrawl_RoundTrips_The_Settings_Object()
    {
        var trawl = new TrawlSettings
        {
            Disabled = true,
            VirtualUsersOverride = 8,
            MaxDurationSecondsOverride = 20,
            WarmupSecondsOverride = 3
        };

        var settings = RunSettingsBuilder.CreateBuilder().WithTrawl(trawl).Build();

        settings.TrawlSettings.Disabled.ShouldBeTrue();
        settings.TrawlSettings.VirtualUsersOverride.ShouldBe(8);
        settings.TrawlSettings.MaxDurationSecondsOverride.ShouldBe(20.0);
        settings.TrawlSettings.WarmupSecondsOverride.ShouldBe(3.0);
    }

    [Fact]
    public void Builder_Granular_Trawl_Helpers_Compose()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithTrawlVirtualUsers(16)
            .WithTrawlMaxDuration(45)
            .WithTrawlWarmup(7)
            .WithTrawlFailOnRegression()
            .DisableTrawl()
            .Build();

        settings.TrawlSettings.VirtualUsersOverride.ShouldBe(16);
        settings.TrawlSettings.MaxDurationSecondsOverride.ShouldBe(45.0);
        settings.TrawlSettings.WarmupSecondsOverride.ShouldBe(7.0);
        settings.TrawlSettings.FailOnRegression.ShouldBeTrue();
        settings.TrawlSettings.Disabled.ShouldBeTrue();
    }

    [Fact]
    public void Builder_WithTrawlFailOnRegression_CanDisableExplicitly()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithTrawlFailOnRegression(false)
            .Build();

        settings.TrawlSettings.FailOnRegression.ShouldBeFalse();
    }
}
