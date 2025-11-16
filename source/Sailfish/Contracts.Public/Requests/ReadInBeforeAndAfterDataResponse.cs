namespace Sailfish.Contracts.Public.Requests;

public record ReadInBeforeAndAfterDataResponse
{
    public ReadInBeforeAndAfterDataResponse(TestData? BeforeData, TestData? AfterData)
    {
        this.BeforeData = BeforeData;
        this.AfterData = AfterData;
    }

    public static ReadInBeforeAndAfterDataResponse CreateNullResponse()
    {
        return new ReadInBeforeAndAfterDataResponse(null, null);
    }

    public TestData? BeforeData { get; init; }
    public TestData? AfterData { get; init; }

    public void Deconstruct(out TestData? BeforeData, out TestData? AfterData)
    {
        BeforeData = this.BeforeData;
        AfterData = this.AfterData;
    }
}