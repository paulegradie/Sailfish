namespace Sailfish.Presentation.Console;

internal interface IPresentationStringConstructor
{
    void AppendLine(string item);
    void AppendLine();
    string Build();
}