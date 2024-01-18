namespace Sailfish.Contracts.Public.Requests;

public class ReadInBeforeAndAfterDataResponse(TestData? beforeData, TestData? afterData)
{
    public TestData? BeforeData { get; } = beforeData;
    public TestData? AfterData { get; } = afterData;

    public static ReadInBeforeAndAfterDataResponse CreateNullResponse()
    {
        return new ReadInBeforeAndAfterDataResponse(null, null);
    }
}