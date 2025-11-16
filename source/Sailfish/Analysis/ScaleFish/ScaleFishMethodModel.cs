using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFishMethodModels
{
    string TestMethodName { get; set; }
    IEnumerable<ScaleFishPropertyModel> ScaleFishPropertyModels { get; set; }
}

public class ScaleFishMethodModel : IScaleFishMethodModels
{
    public ScaleFishMethodModel(string testMethodName, IEnumerable<ScaleFishPropertyModel> scaleFishPropertyModels)
    {
        TestMethodName = testMethodName;
        ScaleFishPropertyModels = scaleFishPropertyModels;
    }

    public string TestMethodName { get; set; }
    public IEnumerable<ScaleFishPropertyModel> ScaleFishPropertyModels { get; set; }

    public static IEnumerable<ScaleFishMethodModel> ParseResult(IEnumerable<KeyValuePair<string, ComplexityProperty>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScaleFishMethodModel(x.Key.Split('.').Last(), ScaleFishPropertyModel.ParseResult(x.Value)));
    }
}