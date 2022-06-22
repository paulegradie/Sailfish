using System.Text;
using VeerPerforma.Presentation.Console;

namespace VeerPerforma.Presentation;

public class PresentationStringConstructor : IPresentationStringConstructor
{
    private readonly StringBuilder stringBuilder;

    public PresentationStringConstructor()
    {
        stringBuilder = new StringBuilder();
    }

    public void AppendLine(string item)
    {
        stringBuilder.AppendLine(item);
    }

    public void AppendLine()
    {
        stringBuilder.AppendLine();
    }

    public string Build()
    {
        return stringBuilder.ToString();
    }
}