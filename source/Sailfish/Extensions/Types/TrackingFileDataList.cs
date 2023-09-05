using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;

namespace Sailfish.Extensions.Types;

public class TrackingFileDataList : List<List<IClassExecutionSummary>>
{
    public List<IClassExecutionSummary> NextTrackingFileData()
    {
        return this.First();
    }

    public void SetTrackingFileData(List<IClassExecutionSummary> classExecutionSummaries)
    {
        Clear();
        Add(classExecutionSummaries);
    }

    public bool PopTrackingFile(out List<IClassExecutionSummary> trackingFileData)
    {
        trackingFileData = this.First();
        var updated = this.Skip(1).ToList();
        Clear();
        AddRange(updated);
        return (Count > 0);
    }
}