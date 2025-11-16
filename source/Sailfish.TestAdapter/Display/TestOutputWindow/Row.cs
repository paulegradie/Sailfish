namespace Sailfish.TestAdapter.Display.TestOutputWindow;

public record Row
{
    public Row(object Item, string Name)
    {
        this.Item = Item;
        this.Name = Name;
    }

    public object Item { get; init; }
    public string Name { get; init; }

    public void Deconstruct(out object Item, out string Name)
    {
        Item = this.Item;
        Name = this.Name;
    }
}