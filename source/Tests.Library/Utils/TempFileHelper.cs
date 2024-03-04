using System.IO;

namespace Tests.Library.Analysis;

public static class TempFileHelper
{
    public static string WriteStringToTempFile(string content)
    {
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, content);
        return tempFilePath;
    }
}