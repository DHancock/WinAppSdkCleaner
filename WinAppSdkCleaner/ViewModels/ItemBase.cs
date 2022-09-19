namespace WinAppSdkCleaner.ViewModels;

internal abstract class ItemBase : INotifyPropertyChanged
{
    private bool isSelected = false;
    private bool isExpanded = false;
    private bool isEnabled = true;

    public ItemBase(ItemBase? parent)
    {
        Parent = parent;
    }

    public List<ItemBase> Children { get; } = new List<ItemBase>();
    public abstract string HeadingText { get; }
    public abstract string ToolTipText { get; }
    public ItemBase? Parent { get; init; }
    public bool HasChildren => Children.Count > 0;

    public bool IsSelected
    {
        get => isSelected;

        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                RaisePropertyChanged();
            }
        }
    }

    public bool IsExpanded
    {
        get => isExpanded;

        set
        {
            if (isExpanded != value)
            {
                isExpanded = value;
                RaisePropertyChanged();
            }
        }
    }

    public bool IsEnabled
    {
        get => isEnabled;

        set
        {
            if (isEnabled != value)
            {
                isEnabled = value;
                RaisePropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

