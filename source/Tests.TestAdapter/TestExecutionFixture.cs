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
    private readonly ContainerBuilder builder;
    private readonly IFrameworkHandle frameworkHandle;
    private readonly List<TestCase> testCases;

    public TestExecutionFixture()
    {
        frameworkHandle = Substitute.For<IFrameworkHandle>();
        builder = new ContainerBuilder();
        builder.RegisterSailfishTypes(Substitute.For<IRunSettings>(), new TestAdapterRegistrations(frameworkHandle));
        builder.RegisterInstance(RunSettingsBuilder.CreateBuilder().DisableOverheadEstimation().Build());
        var projectDll = DllFinder.FindThisProjectsDllRecursively();
        testCases = new TestDiscovery().DiscoverTests(new[] { projectDll }, Substitute.For<IMessageLogger>()).ToList();
        var refTestType = TestExecutor.RetrieveReferenceTypeForTestProject(testCases);
        SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                builder,
                new[] { refTestType },
                new[] { refTestType },
                CancellationToken.None)
            .Wait(CancellationToken.None);
    }

    [Fact]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        using var container = builder.Build();
        Should.NotThrow(() => new TestExecution().ExecuteTests(testCases.Take(1).ToList(), container, frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public void TestCasesAreExecutedCorrectly()
    {
        using var container = builder.Build();
        Should.NotThrow(() => new TestExecution().ExecuteTests(testCases, container, frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public async Task TaskExecutionShouldThrow()
    {
        var program = Substitute.For<ITestAdapterExecutionProgram>();
        var ct = new CancellationToken();
        program.Run(testCases, ct).ThrowsForAnyArgs(new Exception("Test"));
        var b = new ContainerBuilder();
        b.RegisterInstance(program);
        await using var container = b.Build();

        var execution = new TestExecution();
        Should.Throw<Exception>(() => execution.ExecuteTests(testCases, container, frameworkHandle, ct));
    }

    [Fact]
    public void StartupExceptionsAreHandled()
    {
        var context = Substitute.For<IRunContext>();
        using var container = builder.Build();

        var execution = Substitute.For<ITestExecution>();
        execution
            .When(x => x.ExecuteTests(Arg.Any<List<TestCase>>(), Arg.Any<IContainer>(), frameworkHandle, Arg.Any<CancellationToken>()))
            .Do(call => throw new Exception("Oopsie"));
        var executor = new TestExecutor(execution);

        executor.RunTests(testCases, context, frameworkHandle);

        var calls = frameworkHandle.ReceivedCalls();
        calls.Count().ShouldBe(4);
    }
}