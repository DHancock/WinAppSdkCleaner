namespace WinAppSdkCleaner.Utils;

internal static class LogicalTreeHelper
{
    public static IEnumerable<UIElement> GetChildren(UIElement parent)
    {
        if (parent is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                yield return child;
            }
        }
        else if ((parent is UserControl uc) && (uc.Content is not null)) // i.e. a Page
        {
            yield return uc.Content;
        }
        else if ((parent is Border border) && (border.Child is not null))
        {
            yield return border.Child;
        }
        else if (parent is NavigationView navigationView)
        {
            if (navigationView.Content is UIElement content)
            {
                yield return content;
            }

            foreach (object child in navigationView.MenuItems)
            {
                if (child is UIElement uiElement)
                {
                    yield return uiElement;
                }
            }

            foreach (object child in navigationView.FooterMenuItems)
            {
                if (child is UIElement uiElement)
                {
                    yield return uiElement;
                }
            }
        }
        else if (parent is ContentControl contentControl)  // i.e. a Frame, Expander etc
        {
            if (contentControl.Content is Panel ccPanel)
            {
                foreach (UIElement child in ccPanel.Children)
                {
                    yield return child;
                }
            }
            else if (contentControl.Content is UIElement uie)
            {
                yield return uie;
            }
        }
    }
}
