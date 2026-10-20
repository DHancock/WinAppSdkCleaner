using Microsoft.UI.Xaml.Controls.Primitives;

namespace WinAppSdkCleaner.Utilities;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    private static Point GetOffsetFromXamlRoot(UIElement e)
    {
        return e.TransformToVisual(e.XamlRoot.Content).TransformPoint(default);
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, in float scale)
    {
        Debug.Assert(location.X >= 0.0);
        Debug.Assert(location.Y >= 0.0);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)MathF.FusedMultiplyAdd((float)location.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd((float)location.Y, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.Y, scale, 0.5f));
    }

    public static RectInt32 GetPassthroughRect(UIElement e)
    {
        return ScaledRect(GetOffsetFromXamlRoot(e), e.ActualSize, (float)e.XamlRoot.RasterizationScale);
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
                            }

                            return true;
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

    public static int FindSuitablePlacementIndex(ListView list)
    {
        Debug.Assert(list.SelectedRanges.Count > 0);
        Debug.Assert(list.SelectionMode is ListViewSelectionMode.Extended);

        if ((FocusManager.GetFocusedElement(list.XamlRoot) is ListViewItem lvi) && lvi.IsSelected)
        {
            int index = list.IndexFromContainer(lvi);

            if (index > -1) // could be more than one list
            {
                return index;
            }
        }

        ItemsStackPanel isp = (ItemsStackPanel)list.ItemsPanelRoot;

        foreach (ItemIndexRange iir in list.SelectedRanges)
        {
            if ((iir.FirstIndex >= isp.FirstVisibleIndex) && (iir.FirstIndex <= isp.LastVisibleIndex))
            {
                return iir.FirstIndex;
            }

            if ((iir.LastIndex >= isp.FirstVisibleIndex) && (iir.LastIndex <= isp.LastVisibleIndex))
            {
                return isp.FirstVisibleIndex;
            }

            if ((iir.FirstIndex <= isp.FirstVisibleIndex) && (iir.LastIndex >= isp.LastVisibleIndex))
            {
                return isp.FirstVisibleIndex;
            }
        }

        // no obvious overlapps
        return list.SelectedRanges[0].FirstIndex;
    }

    public static void CloseMenuPopups(XamlRoot xamlRoot)
    {
        foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
        {
            if (popup.Child is MenuFlyoutPresenter) // avoid content dialogs
            {
                popup.IsOpen = false;
            }
        }
    }
}