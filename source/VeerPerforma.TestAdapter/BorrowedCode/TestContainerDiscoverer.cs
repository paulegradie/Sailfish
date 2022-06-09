using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.ComponentModel.Composition;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.TestAdapter.BorrowedCode;

namespace VeerPerforma.TestAdapter;

[Export(typeof(ITestContainerDiscoverer))]
public class TestContainerDiscoverer : ITestContainerDiscoverer
{
    public Uri ExecutorUri => TestExecutor.ExecutorUri;
    public IEnumerable<ITestContainer> TestContainers => GetContainers();
    public event EventHandler TestContainersUpdated;


    private IServiceProvider serviceProvider;

    [ImportingConstructor]
    public TestContainerDiscoverer([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    private IEnumerable<ITestContainer> GetContainers()
    {
        var containers = new List<ITestContainer>();

        Parallel.ForEach(GetTestFiles(), filePath => { containers.Add(new VeerPerformaTestContainer(this, filePath)); });

        return containers;
    }

    private IEnumerable<string> GetTestFiles()
    {
        var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
        var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

        return loadedProjects.SelectMany(GetTestFiles).ToList();
    }

    private IEnumerable<string> GetTestFiles(IVsProject project)
    {
        return VsSolutionHelper.GetProjectItems(project).Where(o => IsTestFile(o));
    }

    private bool IsTestFile(string filePath)
    {
        using var reader = new StreamReader(filePath);
        var content = reader.ReadToEnd();
        return content.Contains($"[{nameof(VeerPerformaAttribute).Replace("Attribute", "")}]");
    }
}