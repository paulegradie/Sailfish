namespace VeerPerforma.Execution;

public class TestValidationResult
{
    private TestValidationResult(bool isValid, Type[] tests, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
        Tests = tests;
    }

    public Type[] Tests { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; }

    public static TestValidationResult CreateSuccess(Type[] tests)
    {
        return new TestValidationResult(true, tests, new List<string>());
    }

    public static TestValidationResult CreateFailure(Type[] tests, List<string> errors)
    {
        return new TestValidationResult(false,  tests, errors);
    }
}