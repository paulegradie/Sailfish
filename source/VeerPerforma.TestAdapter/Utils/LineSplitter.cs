﻿using System.Linq;
using System.Text.RegularExpressions;

namespace VeerPerforma.TestAdapter.Utils;

public static class LineSplitter
{
    public static string[] SplitFileIntoLines(string fileContents)
    {
        var newLinesRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
        var lines = newLinesRegex.Split(fileContents).Select(x => x.Trim()).ToArray();
        return lines;
    }
}