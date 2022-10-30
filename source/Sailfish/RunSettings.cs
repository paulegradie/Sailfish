using System;
using Accord.Collections;
using Sailfish.Presentation.TTest;

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
    public Type[] TestLocationTypes { get; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; }
    public string BeforeTarget { get; }
    public DateTime? TimeStamp { get; }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        string trackingDirectoryPath,
        bool noTrack,
        bool analyze,
        bool notify,
        TestSettings settings,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string>  args,
        string beforeTarget,
        DateTime? timeStamp,
        params Type[] testLocationTypes)
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
        Notify = notify;
        TestLocationTypes = testLocationTypes;
    }
}