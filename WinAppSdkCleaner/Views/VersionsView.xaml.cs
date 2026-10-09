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
        if (VersionListView.SelectedRanges.Count > 0)
        {
            if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
            {
                int index = GetFirstPlacementIndex();

                if ((index >= 0) && (index < VersionListView.Items.Count))
                {
                    VersionListView.ScrollIntoView(VersionListView.Items[index]);

                    if (VersionListView.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        Grid grid = (Grid)lvi.ContentTemplateRoot;
                        grid.ContextFlyout.ShowAt(lvi);
                    }
                }

                return true;
            }
            else if (VersionListView.ContainerFromIndex(((ItemsStackPanel)VersionListView.ItemsPanelRoot).FirstVisibleIndex) is ListViewItem lvi)
            {
                // using the first visible index ensures that it's container exists, all menu items use the list view's selected items
                Grid grid = (Grid)lvi.ContentTemplateRoot;
                return Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)grid.ContextFlyout).Items, modifiers, key);
            }
        }

        return true;

        int GetFirstPlacementIndex()
        {
            int index = int.MaxValue;

            // the selected ranges do seem to be sorted but it isn't documented
            foreach (ItemIndexRange itemIndexRange in VersionListView.SelectedRanges)
            {
                if (itemIndexRange.FirstIndex < index)
                {
                    index = itemIndexRange.FirstIndex;
                }
            }

            return index;
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
