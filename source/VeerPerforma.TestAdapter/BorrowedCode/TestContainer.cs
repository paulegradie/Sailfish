using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace VeerPerforma.TestAdapter.BorrowedCode
{
    public class VeerPerformaTestContainer : ITestContainer
    {
        private readonly string source;
        private readonly ITestContainerDiscoverer discoverer;
        private readonly DateTime timeStamp;

        public VeerPerformaTestContainer(ITestContainerDiscoverer discoverer, string source)
        {
            this.discoverer = discoverer;
            this.source = source;
            this.timeStamp = GetTimeStamp();
        }

        private VeerPerformaTestContainer(VeerPerformaTestContainer copy)
            : this(copy.discoverer, copy.Source)
        {
        }

        private DateTime GetTimeStamp()
        {
            if (!string.IsNullOrEmpty(this.Source) && File.Exists(this.Source))
            {
                return File.GetLastWriteTime(this.Source);
            }
            else
            {
                return DateTime.MinValue;
            }
        }


        public int CompareTo(ITestContainer other)
        {
            var testContainer = other as VeerPerformaTestContainer;
            if (testContainer == null)
            {
                return -1;
            }

            var result = String.Compare(this.Source, testContainer.Source, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            return this.timeStamp.CompareTo(testContainer.timeStamp);
        }

        public IEnumerable<Guid> DebugEngines => Enumerable.Empty<Guid>();

        public IDeploymentData? DeployAppContainer()
        {
            return null;
        }

        public ITestContainerDiscoverer Discoverer => this.discoverer;

        public bool IsAppContainerTestContainer => false;

        public ITestContainer Snapshot()
        {
            return new VeerPerformaTestContainer(this);
        }

        public string Source => this.source;

        public Microsoft.VisualStudio.TestPlatform.ObjectModel.FrameworkVersion TargetFramework => Microsoft.VisualStudio.TestPlatform.ObjectModel.FrameworkVersion.None;

        public Microsoft.VisualStudio.TestPlatform.ObjectModel.Architecture TargetPlatform => Microsoft.VisualStudio.TestPlatform.ObjectModel.Architecture.AnyCPU;

    }
}