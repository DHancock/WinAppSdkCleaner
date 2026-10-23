namespace WinAppSdkCleaner.Utilities;

internal static class Extensions
{
    public static T? FindChild<T>(this DependencyObject parent, string? name = null) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);

        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);

            try
            {
                if ((child is T target) && ((name is null) || string.Equals(target.Name, name, StringComparison.Ordinal)))
                {
                    return target;
                }
            }
            catch (InvalidCastException)
            {
            }

            T? result = child.FindChild<T>(name);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    extension(ListViewBase list)
    {
        public bool IsAnySelected => list.SelectedCount > 0;
        public bool IsAllSelected => (list.Items.Count > 0) && (list.SelectedCount == list.Items.Count);

        private long SelectedCount
        {
            get
            {
                if (list.SelectionMode is not ListViewSelectionMode.None)
                {
                    return list.SelectedRanges.Sum(range => range.Length);
                }

                return 0;
            }
        }
    }

    public static void DeselectAll(this ListViewBase list)
    {
        if (list.SelectionMode is not ListViewSelectionMode.None)
        {
            list.DeselectRange(new ItemIndexRange(0, (uint)list.Items.Count));
        }
    }
}
