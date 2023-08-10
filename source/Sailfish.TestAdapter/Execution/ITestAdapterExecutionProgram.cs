using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    void Run(List<TestCase> testCases, CancellationToken cancellationToken);
}