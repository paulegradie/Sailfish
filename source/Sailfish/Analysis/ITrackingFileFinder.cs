

using Sailfish.Extensions.Types;

namespace Sailfish.Analysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary tags);
}