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