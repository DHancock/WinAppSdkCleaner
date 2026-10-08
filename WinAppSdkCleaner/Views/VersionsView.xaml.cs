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

                // scrolling the list item in to view isn't really an option (the sdk list doesn't support it)
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
            RectangleF listBounds = Utils.GetDimensions(VersionListView);
            float groupHeaderHeight = GetGroupHeaderHeight();
            RectangleF listRect = new (listBounds.X + (float)VersionListView.BorderThickness.Left, 
                                       listBounds.Y + groupHeaderHeight + (float)VersionListView.BorderThickness.Top, 
                                       listBounds.Width - (float)(VersionListView.BorderThickness.Left + VersionListView.BorderThickness.Right),
                                       listBounds.Height - (float)(groupHeaderHeight + VersionListView.BorderThickness.Top + VersionListView.BorderThickness.Bottom));

            ListViewItem ? firstItem = null;
            float verticalOffset = float.MaxValue;

            foreach (object item in VersionListView.SelectedItems)
            {
                if (VersionListView.ContainerFromItem(item) is ListViewItem lvi)
                {
                    // If the ListViewItem is scrolled far off the bottom of the listView, it's coordinates will go negative.
                    // The selected items are in selected time order not position in the list (vertical dimension) order.
                    RectangleF itemRect = Utils.GetDimensions(lvi.ContentTemplateRoot);

                    // shrink by 1 pixel
                    itemRect.Inflate(0f, (float)-XamlRoot.RasterizationScale);

                    if ((itemRect.Y < verticalOffset) && itemRect.IntersectsWith(listRect))
                    {
                        verticalOffset = itemRect.Y;
                        firstItem = lvi;
                    }
                }
            }

            return firstItem;
        }

        float GetGroupHeaderHeight()
        {
            if (VersionListView.Items.Count > 0)
            {
                DependencyObject? itemContainer = VersionListView.ContainerFromItem(VersionListView.Items[0]);

                if (itemContainer is not null)
                {
                    DependencyObject? headerContainer = VersionListView.GroupHeaderContainerFromItemContainer(itemContainer);

                    if (headerContainer is UIElement uie)
                    {
                        return uie.ActualSize.Y;
                    }
                }
            }

            Debug.Fail($"{nameof(GetGroupHeaderHeight)} returning default value");
            return 44f;
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
