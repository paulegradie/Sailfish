namespace Tests.E2ETestSuite.Utils;

internal interface IClient
{
    Task Get(CancellationToken cancellationToken);
}