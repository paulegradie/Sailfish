namespace Sailfish.Contracts.Public.Requests;

public class ReadInBeforeAndAfterDataResponse
{
    public TestData? BeforeData { get; }
    public TestData? AfterData { get; }

    public ReadInBeforeAndAfterDataResponse(TestData? beforeData, TestData? afterData)
    {
        BeforeData = beforeData;
        AfterData = afterData;
    }

    public static ReadInBeforeAndAfterDataResponse CreateNullResponse()
    {
        return new ReadInBeforeAndAfterDataResponse(null, null);
    }
}