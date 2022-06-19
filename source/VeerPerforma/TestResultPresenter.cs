using System;
using System.Collections.Generic;
using VeerPerforma.Presentation;
using VeerPerforma.Statistics;

namespace VeerPerforma;

public class TestResultPresenter : ITestResultPresenter
{
    private readonly IConsoleWriter consoleWriter;

    public TestResultPresenter(IConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public void PresentResults(List<CompiledResultContainer> resultContainers)
    {
        foreach (var container in resultContainers)
        {
            if (container.Settings.AsConsole)
            {
                consoleWriter.Present(container);
            }

            if (container.Settings.AsCsv)
            {
                throw new NotImplementedException("Csv writing not yet implemented");
            }
        }
    }
}