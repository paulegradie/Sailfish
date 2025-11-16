namespace Sailfish.TestAdapter.Display.TestOutputWindow;

public record Table
{
    public Table(string Name, double Before, double After)
    {
        this.Name = Name;
        this.Before = Before;
        this.After = After;
    }

    public string Name { get; init; }
    public double Before { get; init; }
    public double After { get; init; }

    public void Deconstruct(out string Name, out double Before, out double After)
    {
        Name = this.Name;
        Before = this.Before;
        After = this.After;
    }
}