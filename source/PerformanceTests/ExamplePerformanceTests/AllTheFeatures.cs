using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Registration;
using Shouldly;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(NumIterations = 2, NumWarmupIterations = 1, DisableOverheadEstimation = false, Disabled = false)]
public class AllTheFeatures
{
    private readonly SomethingIRegistered dep;
    private readonly SomethingYouRegistered reg;
    private readonly MyClient client;

    public AllTheFeatures(SomethingIRegistered dep, SomethingYouRegistered reg, MyClient client)
    {
        this.dep = dep;
        this.reg = reg;
        this.client = client;
    }

    [SailfishRangeVariable(complexity: true, 10, 3, 2)]
    public int Delay { get; set; }

    [SailfishVariable(3, 5)] public int Multiplier { get; set; }


    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        Thread.Sleep(10);
    }

    [SailfishMethod(DisableComplexity = false)]
    public async Task SlowerMethod(CancellationToken ct)
    {
        var wait = Delay - Multiplier * Multiplier;
        Console.WriteLine("Waiting: " + wait);
        await Task.Delay(wait, ct);
        reg.Noop();
    }


    [SailfishMethodSetup(nameof(FasterMethod))]
    public void SpecificSetup()
    {
        // do some setup for the faster method
    }

    [SailfishIterationTeardown]
    public void GlobalIterationTeardown()
    {
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

public class MyClient : ISailfishDependency
{
    private HttpClient client;

    public MyClient()
    {
        // const string apikey = "API-123445";
        // const string url = "http://localhost:1234";
        client = new HttpClient();
    }

    public async Task Get()
    {
        client.CancelPendingRequests();
        await Task.Yield();
    }
}

public class SomethingYouRegistered : ISailfishDependency
{
    public void Noop()
    {
    }
}

public class SomethingIRegistered : ISailfishDependency
{
    public void NowHoldOnJustASec()
    {
        2.ShouldBe(1);
    }
}