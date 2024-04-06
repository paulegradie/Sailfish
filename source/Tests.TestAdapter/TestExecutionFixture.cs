using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish;
using Sailfish.Registration;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Registrations;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        Should.NotThrow(() => TestExecution.ExecuteTests(testCases.Take(1).ToList(), container, frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public void TestCasesAreExecutedCorrectly()
    {
        using var container = builder.Build();
        Should.NotThrow(() => TestExecution.ExecuteTests(testCases, container, frameworkHandle, CancellationToken.None));
    }
}