using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.Execution
{
    public interface ITestCaseIterator
    {
        Task<List<string>> Iterate(TestInstanceContainer testInstanceContainer);
    }
}