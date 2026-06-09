using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
using Sailfish.Analysis.ScaleFish.Trends;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Execution;
using Sailfish.Logging;
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
    private readonly ILogger _logger;
    private readonly IMarkdownTableConverter _markdownTableConverter;
    private readonly IPublisher _publisher;
    private readonly ISender _sender;
    private readonly IRunSettings _runSettings;

    public ScaleFish(IPublisher publisher,
        ISender sender,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        IConsoleWriter consoleWriter,
        ILogger logger)
    {
        _complexityComputer = complexityComputer;
        _consoleWriter = consoleWriter;
        _logger = logger;
        _markdownTableConverter = markdownTableConverter;
        _publisher = publisher;
        _sender = sender;
        _runSettings = runSettings;
    }

    public void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat)
    {
        throw new NotImplementedException();
    }

    // IAnalyzeFromFile entry point — retained for compatibility (ad-hoc / IDE callers). Reads the most
    // recent tracking file, then runs the same core analysis. The run pipeline uses the in-memory overload
    // below instead.
    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;

        var response = await _sender.Send(new GetLatestExecutionSummaryRequest(), cancellationToken);
        await AnalyzeCore(response.LatestExecutionSummaries.ToList(), cancellationToken).ConfigureAwait(false);
    }

    // Decoupled entry point: analyze the CURRENT run's in-memory summaries directly. No tracking-file
    // retrieval, no baseline dependency, and no Type.GetType round-trip — so ScaleFish (and therefore
    // Skipper, which fires off the completion notification) runs on a single run even when SailDiff has no
    // before/after, and the ArgumentNullException("key") from an unresolved test-class Type can't occur.
    public async Task Analyze(IEnumerable<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;
        await AnalyzeCore(executionSummaries.ToList(), cancellationToken).ConfigureAwait(false);
    }

    private async Task AnalyzeCore(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        if (!executionSummaries.Any()) return;

        try
        {
            var analysisResult = _complexityComputer.AnalyzeComplexityWithMeasurements(executionSummaries);
            var complexityResults = analysisResult.Classes.ToList();

            // Runtime backstop for the "frozen scaling variable" trap: warn (never fail) when a
            // scaleFish variable's fit looks ~O(1). Complements the static SF1016 analyzer by also
            // catching indirection that static analysis can't see. Wrapped so a hint never breaks output.
            try
            {
                WarnOnConstantScalingVariables(complexityResults, analysisResult.MeasurementsByPropertyKey);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Debug, "ScaleFish ~O(1) backstop skipped: {0}", ex.Message);
            }

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

            await _publisher.Publish(new ScaleFishAnalysisCompleteNotification(fullMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);

            // Surface complexity-regression transitions via a dedicated notification so downstream
            // consumers (CI scripts, IDE plugins) can react without parsing markdown.
            if (transitions.Any(t => t.IsRegression))
            {
                await _publisher.Publish(new ComplexityRegressionDetectedNotification(
                        transitions.Where(t => t.IsRegression).ToList()),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Surface the failure. A silently-swallowed exception here is exactly why a null-key
            // crash in the complexity computer produced no ScaleFish output — and no error — for
            // so long; log it at Warning so the next regression is visible.
            _logger.Log(LogLevel.Warning, ex, "ScaleFish analysis failed");
            _consoleWriter.WriteString(ex.Message);
        }
    }

    /// <summary>
    /// Emits a Warning-level hint for every scaleFish variable whose already-computed fit looks ~O(1).
    /// Phrased as a question — genuinely constant work is legitimate, so this never fails the run. Reuses
    /// the fit and the measurement vectors already on hand (no curve refitting).
    /// </summary>
    private void WarnOnConstantScalingVariables(
        IReadOnlyList<ScalefishClassModel> complexityResults,
        IReadOnlyDictionary<string, ComplexityMeasurement[]> measurementsByKey)
    {
        foreach (var classModel in complexityResults)
        foreach (var methodModel in classModel.ScaleFishMethodModels)
        foreach (var propModel in methodModel.ScaleFishPropertyModels)
        {
            var model = propModel.ScaleFishModel;
            if (model?.ScaleFishModelFunction is null) continue;

            // PropertyName is the "MethodName.PropertyName" key the computer used to store measurements.
            measurementsByKey.TryGetValue(propModel.PropertyName, out var measurements);

            if (!ConstantComplexityDetector.IsLikelyConstant(model, measurements)) continue;

            // Report the bare variable name (drop the "Method." prefix) so the message reads naturally.
            var variableName = propModel.PropertyName.Split('.').Last();
            _logger.Log(LogLevel.Warning, "{0}", ConstantComplexityDetector.BuildWarningMessage(variableName, model));
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
