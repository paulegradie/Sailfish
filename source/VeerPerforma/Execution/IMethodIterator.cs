using System.Reflection;

namespace VeerPerforma.Execution;

public interface IMethodIterator
{
    Task<List<string>> IterateMethodNTimesAsync(AncillaryInvocation invoker, int numIterations, int numWarmupIterations);
}