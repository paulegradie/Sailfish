namespace Sailfish.Diagnostics.Environment
{
    public interface IEnvironmentHealthReportProvider
    {
        EnvironmentHealthReport? Current { get; set; }
    }

    internal sealed class EnvironmentHealthReportProvider : IEnvironmentHealthReportProvider
    {
        public EnvironmentHealthReport? Current { get; set; }
    }
}

