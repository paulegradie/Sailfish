using System.Collections.Generic;
using System.Threading.Tasks;

namespace VeerPerforma.Execution
{
    public interface IMethodIterator
    {
        Task<List<string>> IterateMethodNTimesAsync(TestInstanceContainer testInstanceContainer);
    }
}