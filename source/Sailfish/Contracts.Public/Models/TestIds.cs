using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Models;

public class TestIds(IEnumerable<string> beforeTestIds, IEnumerable<string> afterTestIds)
{
    public IEnumerable<string> BeforeTestIds { get; set; } = beforeTestIds;
    public IEnumerable<string> AfterTestIds { get; set; } = afterTestIds;
}