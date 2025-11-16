using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishPropertyModels
{
    string PropertyName { get; set; }
    ScaleFishModel ScaleFishModel { get; set; }
}

public class ScaleFishPropertyModel : IScaleFishPropertyModels
{
    public ScaleFishPropertyModel(string propertyName, ScaleFishModel scaleFishModel)
    {
        PropertyName = propertyName;
        ScaleFishModel = scaleFishModel;
    }

    public string PropertyName { get; set; }
    public ScaleFishModel ScaleFishModel { get; set; }

    public static IEnumerable<ScaleFishPropertyModel> ParseResult(Dictionary<string, ScaleFishModel> rawResult)
    {
        return rawResult.Select(x => new ScaleFishPropertyModel(x.Key, x.Value));
    }
}