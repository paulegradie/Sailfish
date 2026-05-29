using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.ScaleFish.Trends;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFish
{
    void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat);
}

internal class ScaleFish : IScaleFish, IScaleFishInternal
{
    private readonly IComplexityComputer _complexityComputer;
    private readonly IConsoleWriter _consoleWriter;
    private readonly IMarkdownTableConverter _markdownTableConverter;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;

    public ScaleFish(IMediator mediator,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        IConsoleWriter consoleWriter)
    {
        _complexityComputer = complexityComputer;
        _consoleWriter = consoleWriter;
        _markdownTableConverter = markdownTableConverter;
        _mediator = mediator;
        _runSettings = runSettings;
    }

    public void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat)
    {
        throw new NotImplementedException();
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;

        var response = await _mediator.Send(new GetLatestExecutionSummaryRequest(), cancellationToken);
        var executionSummaries = response.LatestExecutionSummaries;
        if (!executionSummaries.Any()) return;

        try
        {
            var analysisResult = _complexityComputer.AnalyzeComplexityWithMeasurements(executionSummaries);
            var complexityResults = analysisResult.Classes.ToList();
            var complexityMarkdown = _markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);

            // Optional trend tracking — persist a snapshot of every fit and diff against the most-recent
            // prior snapshot. Failures here are swallowed so a missing tracking directory or a
            // permission error never breaks the headline analysis output.
            IReadOnlyList<ComplexityTransition> transitions = Array.Empty<ComplexityTransition>();
            if (_runSettings.ScaleFishSettings.EnableTrendTracking)
            {
                try
                {
                    transitions = TrackAndDiff(complexityResults);
                }
                catch (Exception ex)
                {
                    _consoleWriter.WriteString($"ScaleFish trend tracking skipped: {ex.Message}");
                }
            }

            var transitionMarkdown = FormatTransitions(transitions);
            var fullMarkdown = string.IsNullOrEmpty(transitionMarkdown)
                ? complexityMarkdown
                : complexityMarkdown + Environment.NewLine + transitionMarkdown;

            _consoleWriter.WriteString(fullMarkdown);

            // Optional standalone HTML report — written alongside the markdown so users can open it
            // directly. Wrapped in try/catch so a filesystem error here never kills the analysis.
            if (_runSettings.ScaleFishSettings.EmitHtmlReport)
            {
                try
                {
                    EmitHtmlReport(complexityResults, analysisResult.MeasurementsByPropertyKey);
                }
                catch (Exception ex)
                {
                    _consoleWriter.WriteString($"ScaleFish HTML report skipped: {ex.Message}");
                }
            }

            await _mediator.Publish(new ScaleFishAnalysisCompleteNotification(fullMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);

            // Surface complexity-regression transitions via a dedicated notification so downstream
            // consumers (CI scripts, IDE plugins) can react without parsing markdown.
            if (transitions.Any(t => t.IsRegression))
            {
                await _mediator.Publish(new ComplexityRegressionDetectedNotification(
                        transitions.Where(t => t.IsRegression).ToList()),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteString(ex.Message);
        }
    }

    private IReadOnlyList<ComplexityTransition> TrackAndDiff(IReadOnlyList<ScalefishClassModel> complexityResults)
    {
        var trackingDir = _runSettings.GetRunSettingsTrackingDirectoryPath();
        var prior = ComplexityHistoryStore.LoadMostRecentPrior(trackingDir);
        var commitSha = ComplexityHistoryStore.ResolveCommitSha();
        var now = DateTime.UtcNow;

        var entries = new List<ComplexityHistoryEntry>();
        foreach (var classModel in complexityResults)
        {
            foreach (var methodModel in classModel.ScaleFishMethodModels)
            {
                foreach (var propModel in methodModel.ScaleFishPropertyModels)
                {
                    entries.Add(HistoryEntryFactory.Build(
                        testClassFullName: $"{classModel.NameSpace}.{classModel.TestClassName}",
                        methodName: methodModel.TestMethodName,
                        propertyName: propModel.PropertyName,
                        model: propModel.ScaleFishModel,
                        commitSha: commitSha,
                        timestampUtc: now));
                }
            }
        }

        if (entries.Count == 0) return Array.Empty<ComplexityTransition>();

        ComplexityHistoryStore.Write(trackingDir, entries, now, commitSha);
        if (prior.Count == 0) return Array.Empty<ComplexityTransition>();
        return ComplexityHistoryDiffer.Diff(prior, entries);
    }

    private void EmitHtmlReport(
        IReadOnlyList<ScalefishClassModel> complexityResults,
        IReadOnlyDictionary<string, ComplexityMeasurement[]> measurementsByKey)
    {
        var html = Sailfish.Presentation.ScaleFishHtmlReportBuilder.Build(complexityResults, measurementsByKey);

        var outputDir = _runSettings.LocalOutputDirectory;
        if (string.IsNullOrWhiteSpace(outputDir)) return;
        if (!System.IO.Directory.Exists(outputDir)) System.IO.Directory.CreateDirectory(outputDir);

        var fileName = $"ScaleFishReport_{DateTime.UtcNow:yyyyMMdd-HHmmss}.html";
        var path = System.IO.Path.Combine(outputDir, fileName);
        System.IO.File.WriteAllText(path, html);
    }

    private static string FormatTransitions(IReadOnlyList<ComplexityTransition> transitions)
    {
        if (transitions.Count == 0) return string.Empty;
        var regressions = transitions.Where(t => t.IsRegression).ToList();
        if (regressions.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## ScaleFish complexity transitions");
        sb.AppendLine();
        sb.AppendLine("| Key | Kind | Summary |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var t in regressions)
        {
            sb.AppendLine($"| {t.Key} | {t.Kind} | {t.Summary} |");
        }
        return sb.ToString();
    }
}
