using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis.Ai;

namespace PerformanceTests.Skipper;

/// <summary>
///     Reference <see cref="ISkipperTransport" /> that drives the local <c>claude</c> CLI as an agentic, code-aware
///     analyst. It is <em>pure transport</em>: Sailfish builds the grounded prompt and parses the reply — this type
///     only pipes the prompt to <c>claude -p</c> (with read-only Read/Grep/Glob scoped to the repository root) and
///     returns the model's raw text. That's the "one seam, two power levels" payoff: the same transport that would
///     accept a one-shot completion here drives a full agent that <em>reads the code under test</em> and cites
///     <c>file:line</c>.
///     <para>
///         This single copy is shared by both demos: the programmatic <c>ConsoleAppDemo</c> and the test-adapter
///         <c>PerformanceTests</c> project each register it with <c>services.AddSkipperTransport&lt;ClaudeCliSkipperTransport&gt;()</c>.
///         Copy it into your own project and swap the transport (Anthropic SDK, Bedrock, a local model) to taste.
///     </para>
///     <para>
///         It degrades by throwing — a missing / offline CLI, a non-zero exit, or a timeout surfaces as an exception,
///         which the framework's agent treats as "Skipper unavailable" and swallows so the benchmark run is never
///         affected.
///     </para>
/// </summary>
public sealed class ClaudeCliSkipperTransport : ISkipperTransport
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public async Task<string> CompleteAsync(string prompt, SkipperSession session, CancellationToken cancellationToken)
    {
        // Scope the CLI's read-only tools to the granted capability's root; fall back to the session root.
        var repositoryRoot = session.Capabilities.Get<ICodeReadCapability>()?.RepositoryRoot ?? session.RepositoryRoot;

        var startInfo = new ProcessStartInfo
        {
            FileName = "claude",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Directory.Exists(repositoryRoot) ? repositoryRoot : Directory.GetCurrentDirectory()
        };
        startInfo.ArgumentList.Add("-p");             // headless "print" mode; the prompt arrives on stdin
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");
        startInfo.ArgumentList.Add("--allowedTools"); // read-only investigation only (space-separated list)
        startInfo.ArgumentList.Add("Read Grep Glob");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.StandardInput.WriteAsync(prompt.AsMemory(), timeout.Token);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"The claude CLI did not respond within {Timeout.TotalSeconds:F0}s.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"claude exited with code {process.ExitCode}: {stderr}");

        return stdout;
    }
}
