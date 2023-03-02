using System;
using System.Collections.Generic;
using Accord.Collections;
using Sailfish.Analysis;

namespace Sailfish;

public interface IRunSettings
{
    IEnumerable<string> TestNames { get; }
    string? LocalOutputDirectory { get; }
    bool CreateTrackingFiles { get; }
    bool Analyze { get; }
    bool Notify { get; set; }
    TestSettings Settings { get; }
    IEnumerable<Type> TestLocationAnchors { get; }
    IEnumerable<Type> RegistrationProviderAnchors { get; }
    OrderedDictionary<string, string> Tags { get; set; }
    OrderedDictionary<string, string> Args { get; }
    IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    DateTime? TimeStamp { get; }
    bool Debug { get; set; }
}

public class RunSettingsBuilder
{
    private List<string> testNames = new();
    private string? localOutputDirectory;
    private bool createTrackingFiles = true;
    private bool analyze = false;

    public static RunSettingsBuilder CreateBuilder()
    {
        return new RunSettingsBuilder();
    }

    public RunSettingsBuilder WithTestNames(params string[] testNames)
    {
        this.testNames.AddRange(testNames);
        return this;
    }

    public RunSettingsBuilder WithLocalOutputDirectory(string localOutputDirectory)
    {
        this.localOutputDirectory = localOutputDirectory;
    }

    public RunSettingsBuilder WithCreateTrackingFiles(bool track = true)
    {
        this.createTrackingFiles = track;
        return this;
    }

    public RunSettingsBuilder WithAnalysis()
    {
        this.analyze = true;
        return this;
    }

    public RunSettingsBuilder WithExecuteNotificationHandler()
    {
        
    }

    public RunSettingsBuilder With()
    {
    }

    public RunSettingsBuilder With()
    {
    }

    public RunSettingsBuilder With()
    {
    }


    public IRunSettings Build()
    {
        return new RunSettings()
        {
        };
    }
}

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
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; }
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
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args,
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
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args,
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
        Tags = new OrderedDictionary<string, string>();
        Args = new OrderedDictionary<string, string>();
        ProvidedBeforeTrackingFiles = Array.Empty<string>();
    }
}