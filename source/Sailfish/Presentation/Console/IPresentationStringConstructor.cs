namespace Sailfish.Presentation.Console;

public interface IPresentationStringConstructor
{
    void AppendLine(string item);
    void AppendLine();
    string Build();
}