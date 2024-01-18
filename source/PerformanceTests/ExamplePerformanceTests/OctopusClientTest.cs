using Octopus.Client;
using Octopus.Client.Model;
using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
///     If you'd like to run a test against server running in your ide - use this format
/// </summary>
[Sailfish(SampleSize = 3)]
public class ExampleUsingAClient
{
    public IOctopusAsyncClient Client { get; set; } = null!;
    public EnvironmentResource Environment { get; set; } = null!;

    [SailfishVariable(true, 10, 50, 100, 500)]
    public int NumMachines { get; set; }

    public List<MachineResource> Machines { get; set; } = new();

    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken ct)
    {
        // Ensure you've started your server on local host and set your api key before running
        Client = await OctopusAsyncClient.Create(new OctopusServerEndpoint("http://localhost:8066", "API-ABC123"));
        Environment = await Client.Repository.Environments.Create(new EnvironmentResource
        {
            Name = Guid.NewGuid().ToString()
        }, ct);
    }

    [SailfishIterationSetup]
    public void IterationSetup()
    {
        Machines.Clear();
        Machines = Enumerable.Range(1, NumMachines)
            .Select(x => new MachineResource
            {
                Name = Guid.NewGuid().ToString(),
                Roles = new ReferenceCollection("MyRole"),
                EnvironmentIds = new ReferenceCollection(Environment.Id),
                Thumbprint = Guid.NewGuid().ToString(),
                Uri = "https://hostname:10933/"
            })
            .ToList();
    }

    [SailfishMethod]
    public async Task MeasureResponseTime(CancellationToken ct)
    {
        await Task.WhenAll(Machines.Select(m => Client.Repository.Machines.Create(m, ct)));
    }

    [SailfishIterationTeardown]
    public async Task DestroyMachinesEachIteration(CancellationToken ct)
    {
        await Task.WhenAll(Machines.Select(m => Client.Repository.Machines.Delete(m, ct)));
    }
}