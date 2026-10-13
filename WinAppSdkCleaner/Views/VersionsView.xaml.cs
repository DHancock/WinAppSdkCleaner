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

    private void SelectAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = VersionListView.SelectedRanges.Sum(range => range.Length) != VersionListView.Items.Count;
    }

    private void SelectAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        VersionListView.SelectRange(new ItemIndexRange(0, (uint)VersionListView.Items.Count));
    }

    public bool InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        // keyboard accelerators only work on the selected items
        if (VersionListView.SelectedRanges.Count > 0)
        {
            if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
            {
                int index = VersionListView.SelectedRanges.Min(range => range.FirstIndex);

                if ((index >= 0) && (index < VersionListView.Items.Count))
                {
                    VersionListView.ScrollIntoView(VersionListView.Items[index]);

                    if (VersionListView.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        lvi.ContextFlyout.ShowAt(lvi);
                        return true;
                    }
                }
            }
            else if (FocusManager.GetFocusedElement(XamlRoot).GetType() != typeof(ListViewItem)
                    && VersionListView.ContainerFromIndex(((ItemsStackPanel)VersionListView.ItemsPanelRoot).FirstVisibleIndex) is ListViewItem lvi)
            {
                // treat as a global hot key, but avoid a double invoke 
                // using the first visible index ensures that it's container exists, all menu items use the list view's selected items
                return Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)lvi.ContextFlyout).Items, modifiers, key);
            }
        }

        return false;
    }

    private void VersionListView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            PointerPoint pointerPoint = e.GetCurrentPoint(null);

            if (pointerPoint.Properties.IsRightButtonPressed)
            {
                object? versionRecord = (e.OriginalSource as FrameworkElement)?.DataContext;

                if (VersionListView.ContainerFromItem(versionRecord) is ListViewItem lvi)
                {
                    if (!lvi.IsSelected)
                    {
                        VersionListView.DeselectRange(new ItemIndexRange(0, (uint)VersionListView.Items.Count));
                        lvi.IsSelected = true;
                    }
                }
            }
        }
    }
}
