namespace Sailfish.Execution;

public class SailfishValidity
{
    private SailfishValidity(bool isValid)
    {
        IsValid = isValid;
    }

    public bool IsValid { get; set; }

    public static SailfishValidity CreateValidResult()
    {
        return new SailfishValidity(true);
    }

    public static SailfishValidity CreateInvalidResult()
    {
        return new SailfishValidity(false);
    }
}