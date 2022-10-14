using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Sailfish.Utils;


namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class LoggerTests
{
    [TestMethod]
    public void ReplaceWorksCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        logger.filePath = tempFile;

        logger.Verbose("What an amazing logger");
        logger.Verbose("This is a {0} test of {wow} the {amazing} logging system", "terrible", "silly", "horrigle");

        using var reader = new StreamReader(tempFile);
        var lines = reader.ReadToEnd().Split("\r").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();

        lines.Length.ShouldBe(2);
        lines.First().ShouldBe("- What an amazing logger");
        lines.Last().ShouldBe("- This is a terrible test of silly the horrigle logging system");
    }
}