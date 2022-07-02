namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationResponse
{
    public BeforeAndAfterFileLocationResponse(string beforeFilePath, string afterFilePath)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
    }

    public string BeforeFilePath { get; set; }
    public string AfterFilePath { get; set; }
}