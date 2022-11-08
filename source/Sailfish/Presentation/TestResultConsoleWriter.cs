using System.Collections.Generic;
using System.Text;
using Sailfish.Analysis;

namespace Sailfish.Presentation;

internal class TestResultConsoleWriter
{
    public static void WriteToConsole(string markdownBody, TestIds testIds, TestSettings testSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, testSettings);
        stringBuilder.AppendLine(markdownBody);
        System.Console.WriteLine(stringBuilder.ToString());
    }

    private static void BuildHeader(StringBuilder stringBuilder, IEnumerable<string> beforeIds, IEnumerable<string> afterIds, TestSettings testSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{testSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {testSettings.Alpha}");
    }
}