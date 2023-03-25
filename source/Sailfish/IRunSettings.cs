using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    OrderedDictionary Tags { get; set; }
    OrderedDictionary Args { get; }
    IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    DateTime? TimeStamp { get; }
    bool Debug { get; set; }
}