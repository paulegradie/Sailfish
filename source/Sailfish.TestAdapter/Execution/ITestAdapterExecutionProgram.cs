using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    void Run(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken);
}