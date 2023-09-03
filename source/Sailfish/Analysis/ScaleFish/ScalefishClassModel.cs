using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScalefishClassModels
{
    string TestClassName { get; set; }
    IEnumerable<ScaleFishMethodModel> ScaleFishMethodModels { get; set; }
}

public class ScalefishClassModel : IScalefishClassModels
{
    public ScalefishClassModel(string testClassName, IEnumerable<ScaleFishMethodModel> scaleFishMethodModels)
    {
        TestClassName = testClassName;
        ScaleFishMethodModels = scaleFishMethodModels;
    }

    public string TestClassName { get; set; }
    public IEnumerable<ScaleFishMethodModel> ScaleFishMethodModels { get; set; }

    public static IEnumerable<IScalefishClassModels> ParseResults(Dictionary<Type, Dictionary<string, Dictionary<string, ScalefishModel>>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScalefishClassModel(x.Key.Name, ScaleFishMethodModel.ParseResult(x.Value)));
    }
}