using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ITestCaseIterator
{
    Task<TestCaseExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, bool DisableOverheadEstimation, CancellationToken cancellationToken);
}