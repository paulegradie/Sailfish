namespace Sailfish.Contracts.Public.Commands;

public class ReadInBeforeAndAfterDataResponse
{
    public TestData? BeforeData { get; }
    public TestData? AfterData { get; }

    public ReadInBeforeAndAfterDataResponse(TestData? beforeData, TestData? afterData)
    {
        BeforeData = beforeData;
        AfterData = afterData;
    }
}