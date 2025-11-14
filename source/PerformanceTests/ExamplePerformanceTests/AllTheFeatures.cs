using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 2, NumWarmupIterations = 1, DisableOverheadEstimation = false, Disabled = false)]
public class AllTheFeatures
{
    private readonly MyClient _client;
    private readonly SomethingIRegistered _dep;
    private readonly SomethingYouRegistered _reg;

    public AllTheFeatures(SomethingIRegistered dep, SomethingYouRegistered reg, MyClient client)
    {
        _dep = dep;
        _reg = reg;
        _client = client;
    }

    [SailfishRangeVariable(true, 10, 3, 2)]
    public int Delay { get; set; }

    [SailfishVariable(3, 5)]
    public int Multiplier { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        Thread.Sleep(10);
    }

    [SailfishMethod(DisableComplexity = false)]
    public async Task SlowerMethod(CancellationToken ct)
    {
        var wait = Delay - Multiplier * Multiplier;
        await Task.Delay(Math.Max(0, wait), ct);
        _reg.Noop();
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
        _reg.Noop();
    }

    [SailfishMethod(Disabled = true)]
    public void ADisabledMethod()
    {
        throw new Exception("WTF");
    }
}

public class MyClient : ISailfishDependency
{
    private readonly HttpClient _client;

    public MyClient()
    {
        // const string apikey = "API-123445";
        // const string url = "http://localhost:1234";
        _client = new HttpClient();
    }

    public async Task Get()
    {
        _client.CancelPendingRequests();
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
        if (2 != 1) throw new Exception();
    }
}