using System;
using System.Diagnostics;

namespace Sailfish.Execution;

/// <summary>
///     Opt-in, best-effort process environment control applied for the duration of a measured class:
///     raise the process priority (to reduce scheduler preemption) and optionally pin the process to a
///     single CPU core (to reduce cross-core migration jitter). Every change is captured on construction
///     and reverted on <see cref="Dispose" />. Any platform denial is swallowed — environment control is
///     an accuracy aid, never a requirement, so a failure to apply it must never fail a run.
///     <para>
///         Note: this complements an eventual out-of-process model. In-process it changes the whole host
///         (which is why it is opt-in); out-of-process the same control can be applied to the dedicated
///         child process without affecting the host.
///     </para>
/// </summary>
internal sealed class EnvironmentControlScope : IDisposable
{
    private readonly bool _pinAffinity;
    private readonly bool _raisePriority;

    private bool _affinityChanged;
    private IntPtr _originalAffinity;
    private ProcessPriorityClass? _originalPriority;

    public EnvironmentControlScope(bool raiseProcessPriority, bool pinToSingleCore)
    {
        _raisePriority = raiseProcessPriority;
        _pinAffinity = pinToSingleCore;
        if (_raisePriority) TryRaisePriority();
        if (_pinAffinity) TryPinAffinity();
    }

    public bool PriorityRaised => _originalPriority.HasValue;
    public bool AffinityPinned => _affinityChanged;

    public void Dispose()
    {
        try
        {
            var p = Process.GetCurrentProcess();
            // _affinityChanged is only ever set on Windows/Linux, but guard explicitly so the platform
            // analyzer can prove the ProcessorAffinity access is safe.
            if (_affinityChanged && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
                p.ProcessorAffinity = _originalAffinity;
            if (_originalPriority.HasValue) p.PriorityClass = _originalPriority.Value;
        }
        catch
        {
            // best-effort revert
        }
    }

    private void TryRaisePriority()
    {
        try
        {
            var p = Process.GetCurrentProcess();
            var current = p.PriorityClass;
            // Only raise (never lower) and only from the unprivileged tiers, so we don't stomp a host
            // that has already chosen High/RealTime.
            if (current is ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal or ProcessPriorityClass.Idle)
            {
                p.PriorityClass = ProcessPriorityClass.High;
                _originalPriority = current;
            }
        }
        catch
        {
            _originalPriority = null;
        }
    }

    private void TryPinAffinity()
    {
        try
        {
            // ProcessorAffinity is only supported on Windows and Linux.
            if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux())) return;

            var p = Process.GetCurrentProcess();
            var original = p.ProcessorAffinity;
            var mask = (long)original;
            // Pin to the lowest core currently permitted by the affinity mask.
            var lowest = mask & -mask;
            if (lowest == 0) lowest = 1;
            p.ProcessorAffinity = (IntPtr)lowest;
            _originalAffinity = original;
            _affinityChanged = true;
        }
        catch
        {
            _affinityChanged = false;
        }
    }
}
