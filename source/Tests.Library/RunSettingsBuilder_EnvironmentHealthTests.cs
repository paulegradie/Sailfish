using Sailfish;
using Shouldly;
using Xunit;

namespace Tests.Library;

public class RunSettingsBuilder_EnvironmentHealthTests
{
    [Fact]
    public void WithEnvironmentHealthCheck_Disables_When_Set_To_False()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithEnvironmentHealthCheck(false)
            .Build();

        settings.EnableEnvironmentHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void WithEnvironmentHealthCheck_Default_Is_Enabled()
    {
        var settings = RunSettingsBuilder.CreateBuilder().Build();
        settings.EnableEnvironmentHealthCheck.ShouldBeTrue();
    }
}

