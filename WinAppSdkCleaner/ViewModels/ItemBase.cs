namespace WinAppSdkCleaner.ViewModels;

internal abstract class ItemBase : IComparable<ItemBase>
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
    public abstract string OtherAppsCount { get; }
    public abstract Visibility OtherAppsCountVisibility { get; }
    public abstract ImageSource? Logo { get; }
    public abstract Visibility LogoVisibility { get; }
    public abstract FontWeight HeadingFontWeight { get; }
    public ItemBase? Parent { get; init; }

    public virtual int CompareTo(ItemBase? other)
    {
        if (other is null)
        {
            return -1;
        }

        return string.Compare(HeadingText, other.HeadingText, StringComparison.CurrentCulture);
    }
}

