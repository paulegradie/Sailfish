using System.IO;
using System.Threading.Tasks;
using Sailfish.Analysis;

namespace Sailfish.Presentation;

internal interface IBeforeAndAfterStreamReader
{
    Task<BeforeAndAfterTrackingFiles> ReadBeforeAndAfterStream(FileStream before, FileStream after);
}