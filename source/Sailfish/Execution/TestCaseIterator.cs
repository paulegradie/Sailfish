using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal class TestCaseIterator : ITestCaseIterator
{
    public async Task<List<string>> Iterate(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
    {
        await WarmupIterations(testInstanceContainer, cancellationToken);

        var messages = new List<string>();
        for (var i = 0; i < testInstanceContainer.NumIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.IterationSetup(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.ExecutionMethod(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.IterationTearDown(cancellationToken).ConfigureAwait(false);
        }

        return messages;
    }

    private static async Task WarmupIterations(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
    {
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.IterationSetup(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.ExecutionMethod(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await testInstanceContainer.Invocation.IterationTearDown(cancellationToken).ConfigureAwait(false);
        }
    }
}