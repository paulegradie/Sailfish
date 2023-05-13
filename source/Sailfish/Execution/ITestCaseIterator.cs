using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ITestCaseIterator
{
    Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken);
}