namespace WinAppSdkCleaner.ViewModels;

internal abstract class ItemBase
{
    public ItemBase(ItemBase? parent)
    {
        Parent = parent;
    }

    public bool IsSelected { get; set; } = false;
    public bool IsExpanded { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public List<ItemBase> Children { get; } = new List<ItemBase>();
    public abstract string HeadingText { get; }
    public abstract string ToolTipText { get; }
    public ItemBase? Parent { get; init; }
}

