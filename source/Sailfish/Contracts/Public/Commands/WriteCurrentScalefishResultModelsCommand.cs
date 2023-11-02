using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentScalefishResultModelsCommand : INotification
{
    public WriteCurrentScalefishResultModelsCommand(List<IScalefishClassModels> testClassComplexityResults, DateTime timeStamp)
    {
        TestClassComplexityResults = testClassComplexityResults;
        TimeStamp = timeStamp;
        DefaultFileName = DefaultFileSettings.DefaultScalefishModelFileName(timeStamp);
    }

    public List<IScalefishClassModels> TestClassComplexityResults { get; }
    public DateTime TimeStamp { get; }
    public string DefaultFileName { get; }
}