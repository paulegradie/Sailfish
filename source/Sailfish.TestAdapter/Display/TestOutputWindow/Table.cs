namespace Sailfish.TestAdapter.Display.TestOutputWindow;

public record Table
{
    public Table(string Name, string Before, string After)
    {
        this.Name = Name;
        this.Before = Before;
        this.After = After;
    }

    public string Name { get; init; }
    public string Before { get; init; }
    public string After { get; init; }

    public void Deconstruct(out string Name, out string Before, out string After)
    {
        Name = this.Name;
        Before = this.Before;
        After = this.After;
    }
}
