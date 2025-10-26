namespace WinAppSdkCleaner.ViewModels;

internal abstract class ItemBase : IComparable<ItemBase>
{
    public ItemBase(ItemBase? parent)
    {
        Parent = parent;
    }

    public List<ItemBase> Children { get; } = new List<ItemBase>();
    public abstract string HeadingText { get; }
    public abstract FontWeight HeadingFontWeight { get; }
    public abstract string ToolTipText { get; }
    public abstract int OtherAppsCount { get; }
    public abstract string OtherAppsCountStr { get; }
    public abstract ImageSource? Logo { get; }
    public string AutomationName => string.Concat(HeadingText, OtherAppsCountStr);
    public ItemBase? Parent { get; init; }
    public abstract int CompareTo(ItemBase? other);

    public override string ToString()
    {
        return HeadingText;
    }
}

