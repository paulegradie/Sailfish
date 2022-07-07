using System;
using System.Collections.Generic;
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
    public Dictionary<string, string> Tags { get; set; }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        string trackingDirectoryPath,
        bool noTrack,
        bool analyze,
        bool notify,
        TTestSettings settings,
        Dictionary<string, string> tags,
        params Type[] testLocationTypes)
    {
        TestNames = testNames;
        DirectoryPath = directoryPath;
        TrackingDirectoryPath = trackingDirectoryPath;
        NoTrack = noTrack;
        Analyze = analyze;
        Settings = settings;
        Tags = tags;
        Notify = notify;
        TestLocationTypes = testLocationTypes;
    }
}