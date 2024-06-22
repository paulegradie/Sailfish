namespace Sailfish.Contracts.Public.Requests;

public record ReadInBeforeAndAfterDataResponse(TestData? BeforeData, TestData? AfterData)
{
    public static ReadInBeforeAndAfterDataResponse CreateNullResponse()
    {
        return new ReadInBeforeAndAfterDataResponse(null, null);
    }
}