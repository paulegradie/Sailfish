using System.Collections.Specialized;

namespace Sailfish.Analysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary tags);
}