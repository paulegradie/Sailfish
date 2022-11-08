using System.Collections.Generic;

namespace Sailfish.Analysis;

public class TestIds
{
    public TestIds(IEnumerable<string> beforeTestIds, IEnumerable<string> afterTestIds)
    {
        BeforeTestIds = beforeTestIds;
        AfterTestIds = afterTestIds;
    }

    public IEnumerable<string> BeforeTestIds { get; set; }
    public IEnumerable<string> AfterTestIds { get; set; }
}