using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public class BeforeAndAfterFileLocationResponse(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths)
{
    public IEnumerable<string> BeforeFilePaths { get; } = beforeFilePaths;
    public IEnumerable<string> AfterFilePaths { get; } = afterFilePaths;
}