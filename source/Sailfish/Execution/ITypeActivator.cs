using System;
using Sailfish.Analysis;

namespace Sailfish.Execution;

public interface ITypeActivator
{
    object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled = false);
}