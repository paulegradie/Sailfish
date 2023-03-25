using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sailfish.Analysis;

namespace Sailfish;

internal class RunSettings : IRunSettings
{
    public IEnumerable<string> TestNames { get; }
    public string? LocalOutputDirectory { get; }
    public bool CreateTrackingFiles { get; }
    public bool Analyze { get; }
    public bool Notify { get; set; }
    public TestSettings Settings { get; }
    public IEnumerable<Type> TestLocationAnchors { get; }
    public IEnumerable<Type> RegistrationProviderAnchors { get; }
    public OrderedDictionary Tags { get; set; }
    public OrderedDictionary Args { get; }
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public DateTime? TimeStamp { get; }
    public bool Debug { get; set; }

    public RunSettings(
        IEnumerable<string> testNames,
        string localOutputDirectory,
        bool createTrackingFiles,
        bool analyze,
        bool notify,
        TestSettings settings,
        OrderedDictionary tags,
        OrderedDictionary args,
        IEnumerable<string> providedBeforeTrackingFiles,
        DateTime? timeStamp,
        IEnumerable<Type> testLocationAnchors,
        IEnumerable<Type> registrationProviderAnchors)
    {
        TestNames = testNames;
        LocalOutputDirectory = localOutputDirectory;
        CreateTrackingFiles = createTrackingFiles;
        Analyze = analyze;
        Settings = settings;
        Tags = tags;
        Args = args;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        TimeStamp = timeStamp;
        Debug = false;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
    }

    public RunSettings(
        IEnumerable<string> testNames,
        string localOutputDirectory,
        bool createTrackingFiles,
        bool analyze,
        bool notify,
        TestSettings settings,
        OrderedDictionary tags,
        OrderedDictionary args,
        IEnumerable<string> providedBeforeTrackingFiles,
        DateTime? timeStamp,
        IEnumerable<Type> testLocationAnchors,
        IEnumerable<Type> registrationProviderAnchors,
        bool debug = false)
    {
        TestNames = testNames;
        LocalOutputDirectory = localOutputDirectory;
        CreateTrackingFiles = createTrackingFiles;
        Analyze = analyze;
        Settings = settings;
        Tags = tags;
        Args = args;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        TimeStamp = timeStamp;
        Debug = debug;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
    }

    public RunSettings()
    {
        TestNames = Array.Empty<string>();
        LocalOutputDirectory = null;
        Settings = new TestSettings(0.001, 3);
        TestLocationAnchors = new[] { GetType() };
        RegistrationProviderAnchors = new[] { GetType() };
        Tags = new OrderedDictionary();
        Args = new OrderedDictionary();
        ProvidedBeforeTrackingFiles = Array.Empty<string>();
    }
}