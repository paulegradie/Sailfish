using System.Collections.Generic;
using System.Threading.Tasks;

namespace VeerPerforma.Execution
{
    public interface ITestCaseIterator
    {
        Task<List<string>> Iterate(TestInstanceContainer testInstanceContainer);
    }
}