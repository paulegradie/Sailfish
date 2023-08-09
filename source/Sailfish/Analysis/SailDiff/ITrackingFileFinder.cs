

using Sailfish.Extensions.Types;

namespace Sailfish.Analysis.Saildiff;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary tags);
}