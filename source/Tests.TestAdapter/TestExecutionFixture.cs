using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish;
using Sailfish.Registration;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Registrations;
using Shouldly;
using Tests.TestAdapter.Utils;
using Xunit;
using IRunSettings = Sailfish.Contracts.Public.Models.IRunSettings;

namespace Tests.TestAdapter;

[Collection(AssemblyDiscoveryCollection.Name)]
public class TestExecutionFixture
{
    private readonly IServiceCollection _services;
    private readonly IFrameworkHandle _frameworkHandle;
    private readonly List<TestCase> _testCases;

    public TestExecutionFixture()
    {
        _frameworkHandle = Substitute.For<IFrameworkHandle>();
        _services = new ServiceCollection();
        _services.AddSailfish(Substitute.For<IRunSettings>());
        _services.AddSailfishTestAdapter(_frameworkHandle);
        _services.AddSingleton(RunSettingsBuilder.CreateBuilder().DisableOverheadEstimation().Build());

        var projectDll = DllFinder.FindThisProjectsDllRecursively();
        _testCases = new TestDiscovery().DiscoverTests(new[] { projectDll }, Substitute.For<IMessageLogger>()).ToList();
        var refTestType = TestExecutor.RetrieveReferenceTypeForTestProject(_testCases);
        SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                _services,
                new[] { refTestType },
                new[] { refTestType },
                CancellationToken.None)
            .Wait(CancellationToken.None);
    }

    [Fact]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        using var provider = _services.BuildServiceProvider();
        Should.NotThrow(() => new TestExecution().ExecuteTests(_testCases.Take(1).ToList(), provider, _frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public void TestCasesAreExecutedCorrectly()
    {
        using var provider = _services.BuildServiceProvider();
        Should.NotThrow(() => new TestExecution().ExecuteTests(_testCases, provider, _frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public async Task TaskExecutionShouldThrow()
    {
        var program = Substitute.For<ITestAdapterExecutionProgram>();
        var ct = new CancellationToken();
        program.Run(_testCases, ct).ThrowsForAnyArgs(new Exception("Test"));

        var services = new ServiceCollection();
        services.AddSingleton(program);
        await using var provider = services.BuildServiceProvider();

        var execution = new TestExecution();
        Should.Throw<Exception>(() => execution.ExecuteTests(_testCases, provider, _frameworkHandle, ct));
    }

    [Fact]
    public void StartupExceptionsAreHandled()
    {
        var context = Substitute.For<IRunContext>();

        var execution = Substitute.For<ITestExecution>();
        execution
            .When(x => x.ExecuteTests(Arg.Any<List<TestCase>>(), Arg.Any<IServiceProvider>(), _frameworkHandle, Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("Oopsie"));
        var executor = new TestExecutor(execution);

        executor.RunTests(_testCases, context, _frameworkHandle);

        var calls = _frameworkHandle.ReceivedCalls();
        calls.Count().ShouldBeGreaterThanOrEqualTo(4);
    }
}
