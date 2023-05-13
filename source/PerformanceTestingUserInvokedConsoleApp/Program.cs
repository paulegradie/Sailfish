using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder()
    .TestsFromAssembliesFromAnchorType(typeof(PerformanceTestProjectDiscoveryAnchor))
    .RegistrationProvidersFromAssembliesFromAnchorType(typeof(AppRegistrationProvider))
    .WithAnalysis()
    .WithLocalOutputDirectory("my_custom_directory")
    .Build();
var result = await SailfishRunner.Run(settings);
var not = result.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");