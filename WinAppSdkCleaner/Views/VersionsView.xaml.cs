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
    }

    internal VersionsViewModel ViewModel => viewModel;

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
    }

    public bool InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        // keyboard accelerators only work on the selected items
        if (VersionListView.SelectedItems.Count > 0)
        {
            if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
            {
                ListViewItem? lvi = FirstVisiblePlacementTarget();

                if (lvi is not null)
                {
                    Grid grid = (Grid)lvi.ContentTemplateRoot;
                    grid.ContextFlyout.ShowAt(lvi);
                }

                // scrolling the list item in to view isn't an option
                return true;
            }
            else if (VersionListView.ContainerFromItem(VersionListView.SelectedItems[0]) is ListViewItem lvi)
            {
                Grid grid = (Grid)lvi.ContentTemplateRoot;
                return Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)grid.ContextFlyout).Items, modifiers, key);
            }
        }

        return true;


        ListViewItem? FirstVisiblePlacementTarget()
        {
            RectangleF list = Utils.GetDimensions(VersionListView);

            ListViewItem? firstItem = null;
            float verticalOffset = float.MaxValue;

            foreach (object item in VersionListView.SelectedItems)
            {
                if (VersionListView.ContainerFromItem(item) is ListViewItem lvi)
                {
                    // If the ListViewItem is scrolled far off the bottom of the listView, it's coordinates will go negative.
                    // The selected items are in selected time order not position in the list (vertical dimension) order.
                    RectangleF itemRect = Utils.GetDimensions(lvi);

                    // shrink by 1 pixel
                    itemRect.Inflate(0f, (float)-XamlRoot.RasterizationScale);

                    if ((itemRect.Y < verticalOffset) && itemRect.IntersectsWith(list))
                    {
                        verticalOffset = itemRect.Y;
                        firstItem = lvi;
                    }
                }
            }

            return firstItem;
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            PointerPoint pointerPoint = e.GetCurrentPoint(this);

            if (pointerPoint.Properties.IsRightButtonPressed)
            {
                object? versionRecord = (e.OriginalSource as FrameworkElement)?.DataContext;

                if (VersionListView.ContainerFromItem(versionRecord) is ListViewItem lvi)
                {
                    if (!lvi.IsSelected)
                    {
                        VersionListView.SelectedItems.Clear();
                        lvi.IsSelected = true;
                    }
                }
            }
        }

        base.OnPointerPressed(e);
    }
}
