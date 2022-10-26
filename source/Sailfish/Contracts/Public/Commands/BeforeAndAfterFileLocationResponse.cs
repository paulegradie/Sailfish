using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationResponse
{
    public BeforeAndAfterFileLocationResponse(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
    }

    public IEnumerable<string> BeforeFilePaths { get; set; }
    public IEnumerable<string> AfterFilePaths { get; set; }
}