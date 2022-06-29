using System;
using Sailfish.Presentation.TTest;

namespace Sailfish;

public class RunSettings
{
    public string[] TestNames { get; }
    public string DirectoryPath { get; }
    public bool NoTrack { get; }
    public bool Analyze { get; }
    public TTestSettings Settings { get; }
    public Type[] TestLocationTypes { get; }

    public RunSettings(
        string[] testNames,
        string directoryPath,
        bool noTrack,
        bool analyze,
        TTestSettings settings,
        params Type[] testLocationTypes)
    {
        TestNames = testNames;
        DirectoryPath = directoryPath;
        NoTrack = noTrack;
        Analyze = analyze;
        Settings = settings;
        TestLocationTypes = testLocationTypes;
    }
}