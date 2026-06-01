using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Host-supplied executor for <see cref="ProposedAction" />s. This is the propose / execute safety boundary:
///     Skipper only ever <i>proposes</i> actions; whether any of them run — and behind what approval policy — is
///     entirely the host's decision. No executor is registered locally, so proposed actions are display-only.
///     Reserved for the action-taking automation future.
/// </summary>
public interface IActionExecutor
{
    Task ExecuteAsync(ProposedAction action, CancellationToken cancellationToken);
}
