namespace WinAppSdkCleaner.Utilites;

internal static class Extensions
{
    public static T? FindChild<T>(this DependencyObject parent, string? name = null) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);

        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);

            if ((child is T target) && ((name is null) || string.Equals(target.Name, name, StringComparison.Ordinal)))
            {
                return target;
            }

            T? result = child.FindChild<T>(name);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
