namespace Sailfish.TestAdapter.Utils;

internal class FileAndContent
{
    public FileAndContent(string file, string content)
    {
        File = file;
        Content = content;
    }

    public string File { get; }
    public string Content { get; }
}