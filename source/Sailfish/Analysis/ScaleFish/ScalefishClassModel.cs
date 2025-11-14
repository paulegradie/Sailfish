using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public class ScalefishClassModel
{
    public ScalefishClassModel(string nameSpace, string testClassName, IEnumerable<ScaleFishMethodModel> scaleFishMethodModels)
    {
        NameSpace = nameSpace;
        TestClassName = testClassName;
        ScaleFishMethodModels = scaleFishMethodModels;
    }

    public string NameSpace { get; set; }
    public string TestClassName { get; set; }
    public IEnumerable<ScaleFishMethodModel> ScaleFishMethodModels { get; set; }

    public static IEnumerable<ScalefishClassModel> ParseResults(Dictionary<Type, ComplexityMethodResult> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScalefishClassModel(x.Key.FullName ?? x.Key.Namespace ?? x.Key.Name, x.Key.Name, ScaleFishMethodModel.ParseResult(x.Value)));
    }
}