namespace Tests.E2ETestSuite.Utils;

public class Client : IClient
{
    public async Task Get(CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
    }
}