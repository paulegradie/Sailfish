using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;

namespace Sailfish.Presentation.TTest;

internal interface ITwoTailedTTestWriter
{
    Task<TestResultFormats> ComputeAndConvertToStringContent(TestData beforeTestData, TestData afterTestData, TestSettings settings, CancellationToken cancellationToken);
}