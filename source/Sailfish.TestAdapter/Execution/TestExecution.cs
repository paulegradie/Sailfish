using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Sailfish.TestAdapter.Execution;

public interface ITestExecution
{
    /// <summary>
    ///     Recommended overload — resolves the execution program from an
    ///     <see cref="IServiceProvider"/> built from <see cref="IServiceCollection"/>.
    /// </summary>
    void ExecuteTests(List<TestCase> testCases, IServiceProvider services, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken);

    /// <summary>
    ///     Legacy Autofac-typed overload — kept for backward compatibility with consumers that built their
    ///     own Autofac <see cref="IContainer"/>. Internally this resolves the execution program via Autofac.
    /// </summary>
    [Obsolete("Use the IServiceProvider overload. The IContainer overload is retained for backward compatibility and will be removed in a future major release.", error: false)]
    void ExecuteTests(List<TestCase> testCases, IContainer container, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken);
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

    [Obsolete("Use the IServiceProvider overload. The IContainer overload is retained for backward compatibility and will be removed in a future major release.", error: false)]
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
