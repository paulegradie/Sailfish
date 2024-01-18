using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishMethodModels
{
    string TestMethodName { get; set; }
    IEnumerable<ScaleFishPropertyModel> ScaleFishPropertyModels { get; set; }
}

public class ScaleFishMethodModel(string testMethodName, IEnumerable<ScaleFishPropertyModel> scaleFishPropertyModels) : IScaleFishMethodModels
{
    public string TestMethodName { get; set; } = testMethodName;
    public IEnumerable<ScaleFishPropertyModel> ScaleFishPropertyModels { get; set; } = scaleFishPropertyModels;

    public static IEnumerable<ScaleFishMethodModel> ParseResult(IEnumerable<KeyValuePair<string, ComplexityProperty>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScaleFishMethodModel(x.Key.Split('.').Last(), ScaleFishPropertyModel.ParseResult(x.Value)));
    }
}