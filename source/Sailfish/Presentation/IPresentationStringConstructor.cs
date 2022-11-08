namespace Sailfish.Presentation;

internal interface IPresentationStringConstructor
{
    void AppendLine(string item);
    void AppendLine();
    string Build();
}