using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sailfish.TestAdapter.Execution;

public interface ITestExecution
{
    void ExecuteTests(List<TestCase> testCases, IContainer container, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken);
}

internal class TestExecution : ITestExecution
{
    public void ExecuteTests(List<TestCase> testCases, IContainer container, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        try
        {
            container.Resolve<ITestAdapterExecutionProgram>().Run(testCases, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Error, ex.Message);
            throw;
        }
    }
}