using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VeerPerforma.Presentation.Console;
using VeerPerforma.Presentation.Csv;
using VeerPerforma.Presentation.Markdown;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation;

public class TestResultPresenter : ITestResultPresenter
{
    private readonly IConsoleWriter consoleWriter;
    private readonly IMarkdownWriter markdownWriter;
    private readonly IPerformanceCsvWriter performanceCsvWriter;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;

    public TestResultPresenter(
        IConsoleWriter consoleWriter,
        IMarkdownWriter markdownWriter,
        IPerformanceCsvWriter performanceCsvWriter,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter)
    {
        this.consoleWriter = consoleWriter;
        this.markdownWriter = markdownWriter;
        this.performanceCsvWriter = performanceCsvWriter;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
    }

    public async Task PresentResults(List<CompiledResultContainer> resultContainers, string directoryPath, bool noTrack)
    {
        var fileName = $"PerformanceResults_{DateTime.Now.ToLocalTime().ToString("yyyy-dd-M--HH-mm-ss")}"; // sortable file name with date

        consoleWriter.Present(resultContainers);
        await markdownWriter.Present(resultContainers, Path.Combine(directoryPath, $"{fileName}.md"));
        await performanceCsvWriter.Present(resultContainers, Path.Combine(directoryPath, $"{fileName}.csv"));

        var output = Path.Combine(directoryPath, "tracking_output");
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }
        await performanceCsvTrackingWriter.Present(resultContainers, Path.Combine(output, $"{fileName}.cvs.tracking"), noTrack);
    }
}