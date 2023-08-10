using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetLatestExecutionSummariesHandler : IRequestHandler<SailfishGetLatestExecutionSummariesCommand, SailfishGetLatestExecutionSummariesResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;
    private readonly ILifetimeScope lifetimeScope;

    public SailfishGetLatestExecutionSummariesHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader, ITrackingFileParser trackingFileParser,
        ILifetimeScope lifetimeScope)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
        this.lifetimeScope = lifetimeScope;
    }

    public async Task<SailfishGetLatestExecutionSummariesResponse> Handle(SailfishGetLatestExecutionSummariesCommand request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(request.TrackingDirectory, ascending: false);
        if (trackingFiles.Count == 0) return new SailfishGetLatestExecutionSummariesResponse(new List<IExecutionSummary>());

        var descriptiveStatisticResults = new List<DescriptiveStatisticsResult>();
        if (!await trackingFileParser.TryParse(trackingFiles.First(), descriptiveStatisticResults, cancellationToken))
        {
            return new SailfishGetLatestExecutionSummariesResponse(new List<IExecutionSummary>());
        }

        var summaries = ConvertDescriptiveToSummary(descriptiveStatisticResults);

        return trackingFiles.Count == 0
            ? new SailfishGetLatestExecutionSummariesResponse(new List<IExecutionSummary>())
            : new SailfishGetLatestExecutionSummariesResponse(summaries);
    }

    private List<IExecutionSummary> ConvertDescriptiveToSummary(IEnumerable<DescriptiveStatisticsResult> results)
    {
        var classGroups = results.GroupBy(x => new TestCaseName(x.DisplayName).Parts[0]);
        var types = GetAllTestTypes();
        var executionSummaries = new List<IExecutionSummary>();
        foreach (var classGroup in classGroups)
        {
            var referenceResult = classGroup.First();
            var referenceType = ExtractType(referenceResult, types);
            if (referenceType is null) continue;
            var compiledResults = new List<ICompiledTestCaseResult>();
            foreach (var descriptiveStatisticsResult in classGroup)
            {
                var testCaseId = new TestCaseId(descriptiveStatisticsResult.DisplayName);
                var compiled = new CompiledTestCaseResult(testCaseId, string.Empty, descriptiveStatisticsResult);
                compiledResults.Add(compiled);
            }

            executionSummaries.Add(new ExecutionSummary(referenceType, compiledResults));
        }

        return executionSummaries;
    }

    private List<Type> GetAllTestTypes()
    {
        return lifetimeScope.ComponentRegistry.Registrations
            .SelectMany(registration => registration.Services)
            .OfType<IServiceWithType>()
            .Select(serviceWithType => serviceWithType.ServiceType)
            .Distinct()
            .ToList();
    }

    private Type? ExtractType(DescriptiveStatisticsResult result, IEnumerable<Type> registeredTypes)
    {
        var testCaseName = new TestCaseName(result.DisplayName);
        var className = testCaseName.Parts[0];
        return registeredTypes.SingleOrDefault(x => x.Name.Equals(className));
    }
}