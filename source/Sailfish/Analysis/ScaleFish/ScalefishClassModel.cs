using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface IScalefishClassModels
{
    string TestClassName { get; }
    IEnumerable<ScaleFishMethodModel> ScaleFishMethodModels { get; }
    string NameSpace { get; }
}

public class ScalefishClassModel : IScalefishClassModels
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

    public static IEnumerable<IScalefishClassModels> ParseResults(Dictionary<Type, ComplexityMethodResult> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new ScalefishClassModel(x.Key.FullName ?? x.Key.Namespace ?? x.Key.Name, x.Key.Name, ScaleFishMethodModel.ParseResult(x.Value)));
    }
}