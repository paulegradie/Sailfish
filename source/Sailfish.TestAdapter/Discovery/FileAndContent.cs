using System.Collections.Generic;

namespace Sailfish.TestAdapter.Discovery;

internal class FileAndContent
{
    public FileAndContent(string file, List<string> content)
    {
        File = file;
        Content = content;
    }

    public string File { get; }
    public List<string> Content { get; }
}