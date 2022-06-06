using System.Reflection;

namespace VeerPerforma.Execution;

public interface IMethodIterator
{
    Task IterateMethodNTimesAsync(AncillaryInvocation invoker, int numIterations, int numWarmupIterations);
}