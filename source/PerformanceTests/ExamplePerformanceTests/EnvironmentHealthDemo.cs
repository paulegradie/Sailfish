using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

// Demonstrates the Environment Health Check feature.
// When run via the Sailfish Test Adapter with the health check enabled (default),
// a health summary is written to the Test Output window and a section is added to the session markdown.
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, DisableOverheadEstimation = false, NumWarmupIterations = 10)]
public class EnvironmentHealthDemo
{
    [SailfishMethod(Order = 1)]
    public void Smoke()
    {
        Thread.Sleep(10);
    }
}

         