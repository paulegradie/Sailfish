using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishPropertyModels
{
    string PropertyName { get; set; }
    ScalefishModel ScalefishModel { get; set; }
}

public class ScaleFishPropertyModel(string propertyName, ScalefishModel scalefishModel) : IScaleFishPropertyModels
{
    public string PropertyName { get; set; } = propertyName;
    public ScalefishModel ScalefishModel { get; set; } = scalefishModel;

    public static IEnumerable<ScaleFishPropertyModel> ParseResult(Dictionary<string, ScalefishModel> rawResult)
    {
        return rawResult.Select(x => new ScaleFishPropertyModel(x.Key, x.Value));
    }
}