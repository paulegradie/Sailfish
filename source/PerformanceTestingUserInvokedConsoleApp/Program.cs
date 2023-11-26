using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using PerformanceTests.ExamplePerformanceTests;
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder()
    .TestsFromAssembliesContaining(typeof(PerformanceTestProjectDiscoveryAnchor))
    .WithTestNames(typeof(MinimalTestExample).FullName)
    .ProvidersFromAssembliesContaining(typeof(AppRegistrationProvider))
    .WithSailDiff()
    .WithScalefish()
    .WithGlobalSampleSize(5)
    .DisableStreamingTrackingUpdates()
    .WithLocalOutputDirectory("my_custom_directory")
    .Build();
var result = await SailfishRunner.Run(settings);
var not = result.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");