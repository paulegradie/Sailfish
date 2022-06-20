using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation;

public class TestResultPresenter : ITestResultPresenter
{
    private readonly IConsoleWriter consoleWriter;
    private readonly IMarkdownWriter markdownWriter;

    public TestResultPresenter(IConsoleWriter consoleWriter, IMarkdownWriter markdownWriter)
    {
        this.consoleWriter = consoleWriter;
        this.markdownWriter = markdownWriter;
    }

    public async Task PresentResults(List<CompiledResultContainer> resultContainers)
    {
        var tempDir = $"C:\\Users\\paule\\code\\VeerPerformaRelated\\Output\\test_result.{Guid.NewGuid().ToString()}.md";

        consoleWriter.Present(resultContainers);
        await markdownWriter.Present(resultContainers, tempDir);
    }
}