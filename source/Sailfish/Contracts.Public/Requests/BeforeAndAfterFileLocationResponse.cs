using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public record BeforeAndAfterFileLocationResponse
{
    public BeforeAndAfterFileLocationResponse(IEnumerable<string> BeforeFilePaths,
        IEnumerable<string> AfterFilePaths)
    {
        this.BeforeFilePaths = BeforeFilePaths;
        this.AfterFilePaths = AfterFilePaths;
    }

    public IEnumerable<string> BeforeFilePaths { get; init; }
    public IEnumerable<string> AfterFilePaths { get; init; }

    public void Deconstruct(out IEnumerable<string> BeforeFilePaths, out IEnumerable<string> AfterFilePaths)
    {
        BeforeFilePaths = this.BeforeFilePaths;
        AfterFilePaths = this.AfterFilePaths;
    }
}