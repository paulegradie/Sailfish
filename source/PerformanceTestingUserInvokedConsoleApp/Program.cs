using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder()
    .TestsFromAssembliesFromAnchorType(typeof(PerformanceTestProjectDiscoveryAnchor))
    .RegistrationProvidersFromAssembliesFromAnchorType(typeof(AppRegistrationProvider))
    .Build();
var result = await SailfishRunner.Run(settings);