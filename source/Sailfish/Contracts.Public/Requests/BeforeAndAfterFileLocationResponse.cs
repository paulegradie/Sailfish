using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public record BeforeAndAfterFileLocationResponse(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths);