using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishPropertyModel : IScaleFishPropertyModels
{
    public ScaleFishPropertyModel(string propertyName, ScalefishModel scalefishModel)
    {
        PropertyName = propertyName;
        ScalefishModel = scalefishModel;
    }

    public string PropertyName { get; set; }
    public ScalefishModel ScalefishModel { get; set; }

    public static IEnumerable<ScaleFishPropertyModel> ParseResult(Dictionary<string, ScalefishModel> rawResult)
    {
        return rawResult.Select(x => new ScaleFishPropertyModel(x.Key, x.Value));
    }
}