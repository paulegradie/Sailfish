using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentScalefishResultModelsCommand : INotification
{
    public WriteCurrentScalefishResultModelsCommand(
        List<ITestClassComplexityResult> testClassComplexityResults,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TestClassComplexityResults = testClassComplexityResults;
        LocalOutputDirectory = localOutputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultScalefishModelFileName(timeStamp);
    }

    public List<ITestClassComplexityResult> TestClassComplexityResults { get; }
    public string LocalOutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}