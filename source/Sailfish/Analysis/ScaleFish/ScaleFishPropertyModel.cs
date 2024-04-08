using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishPropertyModels
{
    string PropertyName { get; set; }
    ScaleFishModel ScaleFishModel { get; set; }
}

public class ScaleFishPropertyModel(string propertyName, ScaleFishModel scaleFishModel) : IScaleFishPropertyModels
{
    public string PropertyName { get; set; } = propertyName;
    public ScaleFishModel ScaleFishModel { get; set; } = scaleFishModel;

    public static IEnumerable<ScaleFishPropertyModel> ParseResult(Dictionary<string, ScaleFishModel> rawResult)
    {
        return rawResult.Select(x => new ScaleFishPropertyModel(x.Key, x.Value));
    }
}