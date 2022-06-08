
namespace VeerPerforma.Execution;

public delegate void AdapterCallbackAction(
    Type type,
    int caseIndex,
    int statusCode,
    Exception? exception,
    string[] messages,
    DateTimeOffset startTime,
    DateTimeOffset endTime,
    TimeSpan duration
);
    
    
public interface IVeerTestExecutor
{
    Task<int> Execute(Type[] tests, AdapterCallbackAction? callback = null);
    Task Execute(Type test, AdapterCallbackAction? callback = null);
}