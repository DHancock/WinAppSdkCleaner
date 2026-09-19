using WinAppSdkCleaner.Utilities;
using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
internal sealed partial class VersionsView : Page, IPageItem
{
    private readonly VersionsViewModel viewModel;

    public VersionsView()
    {
        InitializeComponent();

        viewModel = new VersionsViewModel(this.DispatcherQueue);
        Loaded += VersionsView_Loaded;
    }

    private void VersionsView_Loaded(object sender, RoutedEventArgs e)
    {
        // allow keyboard interaction without the need to tab into the list
        VersionListView.Focus(FocusState.Programmatic);
    }

    internal VersionsViewModel ViewModel => viewModel;

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = args.Parameter is not null;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        if (VersionListView.SelectedItems.Contains(args.Parameter))
        {
            VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
        }
        else
        {
            VersionsViewModel.ExecuteCopy([args.Parameter]);
        }
    }

    public bool InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        // keyboard accelerators only work on the selected items
        if (VersionListView.SelectedItems.Count > 0)
        {
            if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
            {
                ListViewItem? lvi = FirstVisibleElement();
                FrameworkElement placementTarget;

                if (lvi is null) // open the flyout at a sensible location
                {
                    placementTarget = VersionListView;
                    lvi = VersionListView.ContainerFromItem(VersionListView.SelectedItems[0]) as ListViewItem;
                }
                else
                {
                    placementTarget = lvi;
                }
                    
                if (lvi is not null)
                {
                    Grid grid = (Grid)lvi.ContentTemplateRoot;
                    grid.ContextFlyout.ShowAt(placementTarget);
                }

                return true;
            }
            else if (VersionListView.ContainerFromItem(VersionListView.SelectedItems[0]) is ListViewItem lvi)
            {
                Grid grid = (Grid)lvi.ContentTemplateRoot;
                return Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)grid.ContextFlyout).Items, modifiers, key);
            }
        }

        return true;
    }

    private ListViewItem? FirstVisibleElement()
    {
        (int top, int bottom) = GetDimensions(VersionListView);

        ListViewItem? firstItem = null;
        int verticalOffset = int.MaxValue;

        foreach (object item in VersionListView.SelectedItems)
        {
            if (VersionListView.ContainerFromItem(item) is ListViewItem lvi)
            {
                // If the ListViewItem is scrolled far off the bottom of the listView, it's coordinates will go negative.
                // The selected items are in selected time order not position in the list (vertical dimension) order.
                Point itemPoint = Utils.GetOffsetFromXamlRoot(lvi);

                if ((itemPoint.Y >= top) && (itemPoint.Y < bottom) && (itemPoint.Y < verticalOffset))
                {
                    verticalOffset = (int)itemPoint.Y;
                    firstItem = lvi;
                }
            }
        }

        return firstItem;
    }

    private static (int top, int bottom) GetDimensions(UIElement e)
    {
        Point location = Utils.GetOffsetFromXamlRoot(e);
        return ((int)location.Y, (int)(location.Y + e.ActualSize.Y));
    }
}
