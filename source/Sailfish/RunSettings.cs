using System;
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
    public TTestSettings Settings { get; }
    public Type[] TestLocationTypes { get; }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        string trackingDirectoryPath,
        bool noTrack,
        bool analyze,
        bool notify,
        TTestSettings settings,
        params Type[] testLocationTypes)
    {
        TestNames = testNames;
        DirectoryPath = directoryPath;
        TrackingDirectoryPath = trackingDirectoryPath;
        NoTrack = noTrack;
        Analyze = analyze;
        Settings = settings;
        Notify = notify;
        TestLocationTypes = testLocationTypes;
    }
}