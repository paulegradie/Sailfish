using System;
using Accord.Collections;
using Sailfish.Analysis;

namespace Sailfish;

public class RunSettings
{
    public string[] TestNames { get; }
    public string DirectoryPath { get; }
    public string TrackingDirectoryPath { get; }

    public bool NoTrack { get; }
    public bool Analyze { get; }
    public bool Notify { get; set; }
    public TestSettings Settings { get; }
    public Type[] TestLocationAnchors { get; }
    public Type[] RegistrationProviderAnchors { get; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; }
    public string BeforeTarget { get; }
    public DateTime? TimeStamp { get; }
    public bool Debug { get; set; }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        string trackingDirectoryPath,
        bool noTrack,
        bool analyze,
        bool notify,
        TestSettings settings,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args,
        string beforeTarget,
        DateTime? timeStamp,
        Type[] testLocationAnchors,
        Type[] registrationProviderAnchors)
    {
        TestNames = testNames;
        DirectoryPath = directoryPath;
        TrackingDirectoryPath = trackingDirectoryPath;
        NoTrack = noTrack;
        Analyze = analyze;
        Settings = settings;
        Tags = tags;
        Args = args;
        BeforeTarget = beforeTarget;
        TimeStamp = timeStamp;
        Debug = false;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
    }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        string trackingDirectoryPath,
        bool noTrack,
        bool analyze,
        bool notify,
        TestSettings settings,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args,
        string beforeTarget,
        DateTime? timeStamp,
        Type[] testLocationAnchors,
        Type[] registrationProviderAnchors,
        bool debug = false)
    {
        TestNames = testNames;
        DirectoryPath = directoryPath;
        TrackingDirectoryPath = trackingDirectoryPath;
        NoTrack = noTrack;
        Analyze = analyze;
        Settings = settings;
        Tags = tags;
        Args = args;
        BeforeTarget = beforeTarget;
        TimeStamp = timeStamp;
        Debug = debug;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
    }

#pragma warning disable CS8618
    private RunSettings()
#pragma warning restore CS8618
    {
        TestLocationAnchors = Array.Empty<Type>();
        RegistrationProviderAnchors = Array.Empty<Type>();
    }

    internal static RunSettings CreateTestAdapterSettings()
    {
        return new RunSettings();
    }
}