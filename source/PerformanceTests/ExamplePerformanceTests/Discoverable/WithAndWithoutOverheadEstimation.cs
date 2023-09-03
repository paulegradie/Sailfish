using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Registration;
using Shouldly;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

public class SomethingYouRegistered : ISailfishDependency
{
    public void Noop()
    {
        
    }
}

public class SomethingIRegistered : ISailfishDependency
{
    private readonly SomethingIRegistered somethingIRegistered;

    public SomethingIRegistered()
    {
        this.somethingIRegistered = somethingIRegistered;
    }

    public void NowHoldOnJustASec()
    {
        2.ShouldBe(1);
    }
}

[WriteToMarkdown]
[Sailfish(NumIterations = 4)]
public class WithAndWithoutOverheadEstimation
{
    private readonly SomethingIRegistered dep;
    private readonly SomethingYouRegistered reg;

    public WithAndWithoutOverheadEstimation(SomethingIRegistered dep, SomethingYouRegistered reg)
    {
        this.dep = dep;
        this.reg = reg;
    }

    [SailfishRangeVariable(30, 20, 2)] public int Delay { get; set; }
    [SailfishVariable(3, 5)] public int Multiplier { get; set; }


    [SailfishMethod]
    public async Task SlowerMethod(CancellationToken ct)
    {
        await Task.Delay(Delay - Multiplier * Multiplier, ct);
        reg.Noop();
    }

    [SailfishMethodSetup(nameof(FasterMethod))]
    public void SpecificSetup()
    {
        // do some setup for the faster method
    }

    [SailfishMethod(DisableOverheadEstimation = true)]
    public async Task FasterMethod(CancellationToken ct)
    {
        await Task.Delay(Delay, ct);
        reg.Noop();
    }
    
    

    [SailfishMethod(Disabled = true)]
    public void ADisabledMethod()
    {
        throw new Exception("WTF");
        reg.Noop();
        dep.NowHoldOnJustASec();
    }
}