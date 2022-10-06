using System.Threading.Tasks;
using Sailfish.Contracts.Public;

namespace Sailfish.Presentation.TTest;

internal interface ITwoTailedTTestWriter
{
    Task<TTestResultFormats> ComputeAndConvertToStringContent(TestData beforeTestData, TestData afterTestData, TTestSettings settings);
}