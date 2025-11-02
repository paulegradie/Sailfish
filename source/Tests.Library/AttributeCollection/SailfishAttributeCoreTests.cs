using System;
using System.Reflection;
using Shouldly;
using Xunit;
using Sailfish.Attributes;

namespace Tests.Library.AttributeCollection;

public class SailfishAttributeCoreTests
{
    [Fact]
    public void InternalDefaultConstructor_SetsExpectedDefaults()
    {
        // Use reflection to invoke the internal parameterless constructor
        var ctor = typeof(SailfishAttribute).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, types: Type.EmptyTypes, modifiers: null);
        ctor.ShouldNotBeNull();
        var attr = (SailfishAttribute)ctor!.Invoke(null);

        attr.SampleSize.ShouldBe(3);
        attr.NumWarmupIterations.ShouldBe(3);
        attr.Disabled.ShouldBeFalse();
        attr.DisableOverheadEstimation.ShouldBeFalse();
        attr.UseAdaptiveSampling.ShouldBeFalse();
        attr.TargetCoefficientOfVariation.ShouldBe(0.05);
        attr.MaximumSampleSize.ShouldBe(1000);
    }

    [Fact]
    public void PublicConstructor_DefaultsAndSetters_Work()
    {
        var attr = new SailfishAttribute();
        attr.SampleSize.ShouldBe(3);
        attr.NumWarmupIterations.ShouldBe(3);
        attr.Disabled.ShouldBeFalse();

        // Set a few knobs
        attr.UseAdaptiveSampling = true;
        attr.TargetCoefficientOfVariation = 0.10;
        attr.MaximumSampleSize = 200;
        attr.DisableOverheadEstimation = true;

        attr.UseAdaptiveSampling.ShouldBeTrue();
        attr.TargetCoefficientOfVariation.ShouldBe(0.10);
        attr.MaximumSampleSize.ShouldBe(200);
        attr.DisableOverheadEstimation.ShouldBeTrue();
    }
}

