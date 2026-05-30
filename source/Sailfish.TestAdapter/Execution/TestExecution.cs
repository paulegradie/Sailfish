using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Sailfish.TestAdapter.Execution;

public interface ITestExecution
{
    void ExecuteTests(List<TestCase> testCases, IServiceProvider services, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken);
}

internal class TestExecution : ITestExecution
{
    public void ExecuteTests(List<TestCase> testCases, IServiceProvider services, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        try
        {
            services.GetRequiredService<ITestAdapterExecutionProgram>().Run(testCases, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Error, ex.Message);
            throw;
        }
    }
}
