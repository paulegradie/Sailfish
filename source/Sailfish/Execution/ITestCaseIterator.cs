using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.Execution
{
    internal interface ITestCaseIterator
    {
        Task<List<string>> Iterate(TestInstanceContainer testInstanceContainer);
    }
}