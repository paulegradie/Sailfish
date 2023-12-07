using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public class ScalefishClassModel(string nameSpace, string testClassName, IEnumerable<ScaleFishMethodModel> scaleFishMethodModels)
{
    public string NameSpace { get; set; } = nameSpace;
    public string TestClassName { get; set; } = testClassName;
    public IEnumerable<ScaleFishMethodModel> ScaleFishMethodModels { get; set; } = scaleFishMethodModels;

    public static IEnumerable<ScalefishClassModel> ParseResults(Dictionary<Type, ComplexityMethodResult> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScalefishClassModel(x.Key.FullName ?? x.Key.Namespace ?? x.Key.Name, x.Key.Name, ScaleFishMethodModel.ParseResult(x.Value)));
    }
}