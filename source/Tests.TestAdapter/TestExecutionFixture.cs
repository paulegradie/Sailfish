using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
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

public class TestExecutionFixture
{
    private readonly ContainerBuilder _builder;
    private readonly IFrameworkHandle _frameworkHandle;
    private readonly List<TestCase> _testCases;

    public TestExecutionFixture()
    {
        _frameworkHandle = Substitute.For<IFrameworkHandle>();
        _builder = new ContainerBuilder();
        _builder.RegisterSailfishTypes(Substitute.For<IRunSettings>(), new TestAdapterRegistrations(_frameworkHandle));
        _builder.RegisterInstance(RunSettingsBuilder.CreateBuilder().DisableOverheadEstimation().Build());
        var projectDll = DllFinder.FindThisProjectsDllRecursively();
        _testCases = new TestDiscovery().DiscoverTests(new[] { projectDll }, Substitute.For<IMessageLogger>()).ToList();
        var refTestType = TestExecutor.RetrieveReferenceTypeForTestProject(_testCases);
        SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                _builder,
                new[] { refTestType },
                new[] { refTestType },
                CancellationToken.None)
            .Wait(CancellationToken.None);
    }

    [Fact]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        using var container = _builder.Build();
        Should.NotThrow(() => new TestExecution().ExecuteTests(_testCases.Take(1).ToList(), container, _frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public void TestCasesAreExecutedCorrectly()
    {
        using var container = _builder.Build();
        Should.NotThrow(() => new TestExecution().ExecuteTests(_testCases, container, _frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public async Task TaskExecutionShouldThrow()
    {
        var program = Substitute.For<ITestAdapterExecutionProgram>();
        var ct = new CancellationToken();
        program.Run(_testCases, ct).ThrowsForAnyArgs(new Exception("Test"));
        var b = new ContainerBuilder();
        b.RegisterInstance(program);
        await using var container = b.Build();

        var execution = new TestExecution();
        Should.Throw<Exception>(() => execution.ExecuteTests(_testCases, container, _frameworkHandle, ct));
    }

    [Fact]
    public void StartupExceptionsAreHandled()
    {
        var context = Substitute.For<IRunContext>();
        using var container = _builder.Build();

        var execution = Substitute.For<ITestExecution>();
        execution
            .When(x => x.ExecuteTests(Arg.Any<List<TestCase>>(), Arg.Any<IContainer>(), _frameworkHandle, Arg.Any<CancellationToken>()))
            .Do(call => throw new Exception("Oopsie"));
        var executor = new TestExecutor(execution);

        executor.RunTests(_testCases, context, _frameworkHandle);

        var calls = _frameworkHandle.ReceivedCalls();
        calls.Count().ShouldBeGreaterThanOrEqualTo(4);
    }
}