using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public interface ICsvCompiler
{
    void CompileToCsv(TestExecutionResult result);
}