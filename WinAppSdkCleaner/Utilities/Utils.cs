namespace WinAppSdkCleaner.Utilities;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static Point GetOffsetFromXamlRoot(UIElement e)
    {
        GeneralTransform gt = e.TransformToVisual(e.XamlRoot.Content);
        return gt.TransformPoint(new Point(0f, 0f));
    }

    public static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        Debug.Assert(location.X >= 0.0);
        Debug.Assert(location.Y >= 0.0);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)Math.FusedMultiplyAdd(location.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(location.Y, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.Y, scale, 0.5));
    }

    public static RectInt32 GetPassthroughRect(UIElement e)
    {
        return ScaledRect(GetOffsetFromXamlRoot(e), e.ActualSize, e.XamlRoot.RasterizationScale);
    }

    public static bool InvokeMenuItemForKeyboardAccelerator(IList<MenuFlyoutItemBase> menuItems, VirtualKeyModifiers modifiers, VirtualKey key)
    {
        foreach (MenuFlyoutItemBase mfib in menuItems)
        {
            if (mfib is MenuFlyoutSubItem subItem)
            {
                if (InvokeMenuItemForKeyboardAccelerator(subItem.Items, modifiers, key))
                {
                    return true;
                }
            }
            else if (mfib is MenuFlyoutItem mfi)
            {
                foreach (KeyboardAccelerator ka in mfib.KeyboardAccelerators)
                {
                    if (ka.IsEnabled && (ka.Modifiers == modifiers) && (ka.Key == key))
                    {
                        Debug.Assert(ka.ScopeOwner is null);

                        if (mfi.Command is not null)
                        {
                            // CanExecute() defines if the action is performed, not the menu item's enabled state
                            // The enabled state is only updated when the menu is shown 
                            if (mfi.Command.CanExecute(mfi.CommandParameter))
                            {
                                mfi.Command.Execute(mfi.CommandParameter);
                                return true;
                            }
                        }
                        else if (mfi.IsEnabled)
                        {
                            // the menu item has a click event handler, it's enabled state would be adjusted in code when required
                            AutomationPeer? ap = FrameworkElementAutomationPeer.FromElement(mfi);
                            MenuFlyoutItemAutomationPeer? ip = ap?.GetPattern(PatternInterface.Invoke) as MenuFlyoutItemAutomationPeer;

                            ip?.Invoke();
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
