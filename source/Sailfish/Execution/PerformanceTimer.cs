using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sailfish.Execution
{
    public class PerformanceTimer
    {
        public readonly List<IterationPerformance> ExecutionIterationPerformances = new();
        private readonly Stopwatch executionTimer;
        private readonly Stopwatch globalTimer;

        public readonly List<IterationPerformance> MethodIterationPerformances = new(); // all iterations of the method
        private readonly Stopwatch methodTimer;
        private DateTimeOffset executionIterationStart;

        // transient fields
        private DateTimeOffset methodIterationStart;

        public PerformanceTimer()
        {
            globalTimer = new Stopwatch();
            methodTimer = new Stopwatch();
            executionTimer = new Stopwatch();
        }

        public DateTimeOffset GlobalStart { get; private set; }
        public DateTimeOffset GlobalStop { get; private set; }
        public TimeSpan GlobalDuration { get; private set; }
        public bool IsValid { get; private set; } = true;

        public void SetAsInvalid()
        {
            IsValid = false;
        }

        public void StartExecutionTimer()
        {
            if (executionTimer.IsRunning) return;
            executionIterationStart = DateTimeOffset.Now;
            executionTimer.Start();
        }

        public void StopExecutionTimer()
        {
            if (!executionTimer.IsRunning) return;
            executionTimer.Stop();
            var executionIterationStop = DateTimeOffset.Now;
            ExecutionIterationPerformances.Add(new IterationPerformance(executionIterationStart, executionIterationStop, executionTimer.ElapsedMilliseconds));
            executionTimer.Reset();
        }

        public void StartMethodTimer()
        {
            if (methodTimer.IsRunning) return;
            methodIterationStart = DateTimeOffset.Now;
            methodTimer.Start();
        }

        public void StopMethodTimer()
        {
            if (!methodTimer.IsRunning) return;
            methodTimer.Stop();
            var methodIterationStop = DateTimeOffset.Now;
            MethodIterationPerformances.Add(new IterationPerformance(methodIterationStart, methodIterationStop, methodTimer.ElapsedMilliseconds));
            methodTimer.Reset();
        }

        public void StartGlobalTimer()
        {
            if (globalTimer.IsRunning) return;
            GlobalStart = DateTimeOffset.Now;
            globalTimer.Start();
        }

        public void StopGlobalTimer()
        {
            if (!globalTimer.IsRunning) return;
            globalTimer.Stop();
            GlobalStop = DateTimeOffset.Now;
            GlobalDuration = GlobalStop - GlobalStart;
            globalTimer.Reset();
        }
    }
}