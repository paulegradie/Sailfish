using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.DefaultHandlers.ScaleFish;
using Shouldly;
using Tests.Common.Builders.Scalefish;
using Tests.Common.Builders.ScaleFish;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.DefaultHandlers;

public class ScaleFishAnalysisCompleteNotificationHandlerTests
{
    [Fact]
    public async Task ScaleFishNotificationIsHandled()
    {
        var testClassName = Some.RandomString();

        var classModels = new List<ScalefishClassModel>
        {
            new(Some.RandomString(), testClassName, new List<ScaleFishMethodModel>
            {
                new(Some.RandomString(), new List<ScaleFishPropertyModel>
                {
                    ScaleFishPropertyModelBuilder.Create().Build()
                })
            })
        };

        var notification = new ScaleFishAnalysisCompleteNotification("This is markdown", classModels);

        var outputDirectory = Some.RandomString();
        var handler = new ScaleFishAnalysisCompleteNotificationHandler(RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build());

        // invoke the handler
        await handler.Handle(notification, CancellationToken.None);

        // assert results
        var files = Directory.GetFiles(outputDirectory);
        files.Length.ShouldBe(2);
        var modelFile = files.Single(x => x.EndsWith("json"));

        var data = await File.ReadAllTextAsync(modelFile);
        var model = SailfishSerializer.Deserialize<List<ScalefishClassModel>>(data);
        model.ShouldNotBeNull();
        model.Count.ShouldBe(1);
        model.Single().TestClassName.ShouldBe(testClassName);
        model.Single().ScaleFishMethodModels.Single().ScaleFishPropertyModels.Single().ScaleFishModel.GoodnessOfFit.ShouldBe(0.98);
    }
}