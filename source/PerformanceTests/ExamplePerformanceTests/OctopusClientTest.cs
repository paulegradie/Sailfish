using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client;
using Octopus.Client.Model;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests
{
    [Sailfish(NumIterations = 3)]
    public class ExampleUsingOctopusClient
    {
        public IOctopusAsyncClient Client { get; set; } = null!;

        [SailfishGlobalSetup]
        public async Task GlobalSetup()
        {
            // Ensure you've started your server on local host and set your api key before running
            Client = await OctopusAsyncClient.Create(new OctopusServerEndpoint("http://localhost:8066", "API-FIJUELJVTGK3TRGDJLJHRPVXC72LGTJ"));
        }

        [SailfishVariable(true, 10, 50, 100, 500)]
        public int NumMachines { get; set; }

        public List<MachineResource> Machines { get; set; } = new();

        [SailfishIterationSetup]
        public void IterationSetup()
        {
            Machines.Clear();
            Machines = Enumerable.Range(1, NumMachines)
                .Select(x => new MachineResource()
                {
                    Name = Guid.NewGuid().ToString()
                })
                .ToList();
        }

        [SailfishMethod]
        public async Task MeasureResponseTime(CancellationToken ct)
        {
            await Task.WhenAll(Machines.Select(m => Client.Repository.Machines.Create(m, ct)));
        }

        [SailfishIterationTeardown]
        public async Task DestroyMachinesEachIteration(CancellationToken ct) => await Task.WhenAll(Machines.Select(m => Client.Repository.Machines.Delete(m, ct)));
    }
}