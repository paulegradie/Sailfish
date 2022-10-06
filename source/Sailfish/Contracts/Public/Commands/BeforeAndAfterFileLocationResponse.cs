using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationResponse
{
    public BeforeAndAfterFileLocationResponse(IEnumerable<string> beforeFilePath, IEnumerable<string> afterFilePath)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
    }

    public IEnumerable<string> BeforeFilePath { get; set; }
    public IEnumerable<string> AfterFilePath { get; set; }
}