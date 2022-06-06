namespace VeerPerforma.Execution;

public class TestValidationResult
{
    private TestValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; set; }
    public List<string> Errors { get; }

    public static TestValidationResult CreateSuccess()
    {
        return new TestValidationResult(true, new List<string>());
    }

    public static TestValidationResult CreateFailure(List<string> errors)
    {
        return new TestValidationResult(false, errors);
    }
}