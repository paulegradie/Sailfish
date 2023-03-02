using Accord.Collections;
using PerformanceTests;
using Sailfish;
using Sailfish.Analysis;

var settings = CreateRunSettings();
var result = await SailfishRunner.Run(settings);



static RunSettings CreateRunSettings()
{
    return new RunSettings(
        testNames: Array.Empty<string>(),
        localOutputDirectory: string.Empty,
        createTrackingFiles: true,
        analyze: true,
        notify: true,
        settings: new TestSettings(0.0001, 6),
        tags: new OrderedDictionary<string, string>(),
        args: new OrderedDictionary<string, string>(),
        providedBeforeTrackingFiles: string.Empty,
        timeStamp: DateTime.Now,
        testLocationAnchors: new[] { typeof(PerformanceTestProjectDiscoveryAnchor) }, // where to find the tests
        registrationProviderAnchors: new[] { typeof(RegistrationProvider) } // where to find the registrations
    );
}