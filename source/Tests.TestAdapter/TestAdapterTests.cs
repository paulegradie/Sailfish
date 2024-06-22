using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Exceptions;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.TestAdapter.Utils;
using Xunit;

namespace Tests.TestAdapter;

public class TestDiscovererTests : IAsyncLifetime
{
    private IDiscoveryContext context = null!;
    private IMessageLogger logger = null!;
    private ITestCaseDiscoverySink sink = null!;


    public Task InitializeAsync()
    {
        context = Substitute.For<IDiscoveryContext>();
        logger = Substitute.For<IMessageLogger>();
        sink = Substitute.For<ITestCaseDiscoverySink>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public void FileExtensionsAreSetCorrectly()
    {
        var extension = typeof(TestDiscoverer)
            .GetCustomAttribute<FileExtensionAttribute>()?
            .FileExtension ?? throw new NullReferenceException();
        extension.ShouldBe(".dll");
    }

    [Fact]
    public void DefaultExecutorUriIsSetCorrectly()
    {
        var uri = typeof(TestDiscoverer)?
            .GetCustomAttribute<DefaultExecutorUriAttribute>()?
            .ExecutorUri ?? throw new NullReferenceException();
        uri.ShouldBe("executor://sailfishexecutor/v1");
    }

    [Fact]
    public void DiscoverTestsReturnsWhenNoTestsDiscovered()
    {
        var sources = new List<string>
            { "testSource" };
        var discoverer = new TestDiscoverer();
        discoverer.DiscoverTests(sources, context, logger, sink);

        var call = logger.ReceivedCalls().First();
        call.GetArguments().Length.ShouldBe(2);
        call.GetArguments()[0].ShouldBe(TestMessageLevel.Warning);
        call.GetArguments()[1].ShouldBe("No tests discovered.");
    }

    [Fact]
    public void ExclusionsAreNotIncluded()
    {
        string[] sources =
        [
            "Sailfish.TestAdapter.dll",
            "Tests.Library.TestAdapter.dll"
        ];
        var discoverer = new TestDiscoverer();
        discoverer.DiscoverTests(sources, context, logger, sink);

        var call = logger.ReceivedCalls().First();
        call.GetArguments().Length.ShouldBe(2);
        call.GetArguments()[0].ShouldBe(TestMessageLevel.Warning);
        call.GetArguments()[1].ShouldBe("No tests discovered.");
    }

    [Fact]
    public void DiscoveryWorkAsExpected()
    {
        var source = DllFinder.FindThisProjectsDllRecursively();
        var discoverer = new TestDiscoverer(new TestDiscovery());
        discoverer.DiscoverTests([source], context, logger, sink);
        sink.ReceivedCalls().Count().ShouldBe(18);
    }

    [Fact]
    public void SailfishExceptionIsThrownWhenExceptionOccurs()
    {
        var source = DllFinder.FindThisProjectsDllRecursively();
        var discovery = Substitute.For<ITestDiscovery>();
        discovery
            .DiscoverTests(Arg.Any<IEnumerable<string>>(), Arg.Any<IMessageLogger>())
            .ThrowsForAnyArgs(new Exception());

        var discoverer = new TestDiscoverer(discovery);

        Should.Throw<SailfishException>(() => discoverer.DiscoverTests([source], context, logger, sink));
    }
}