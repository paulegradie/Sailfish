namespace Tests.E2E.TestSuite.Utils;

internal static class ClientFactory
{
    public static IClient CreateClient(string url)
    {
        return new Client();
    }
}