namespace Tests.E2ETestSuite.Utils;

internal static class ClientFactory
{
    public static IClient CreateClient(string url)
    {
        return new Client();
    }
}