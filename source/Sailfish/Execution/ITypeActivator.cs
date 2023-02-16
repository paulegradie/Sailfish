using System;

namespace Sailfish.Execution;

public interface ITypeActivator
{
    object CreateDehydratedTestInstance(Type test);
}