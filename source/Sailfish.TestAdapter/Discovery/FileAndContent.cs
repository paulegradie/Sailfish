namespace Sailfish.TestAdapter.Discovery;

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