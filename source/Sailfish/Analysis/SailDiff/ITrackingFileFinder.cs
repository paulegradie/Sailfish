

using Sailfish.Extensions.Types;

namespace Sailfish.Analysis.SailDiff;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary tags);
}