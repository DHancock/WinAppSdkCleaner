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

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
         args.CanExecute = VersionListView.IsAnySelected;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
    }

    private void SelectAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        if (VersionListView.IsAllSelected)
        {                                                  
            sender.Label = "Deselect All";
            sender.Description = "Deselects all content";       
        }
        else
        {
            sender.Label = "Select All";
            sender.Description = "Selects all content"; 
        }
    }

    private void SelectAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        if (VersionListView.IsAllSelected)
        {
            VersionListView.DeselectAll();
        }
        else
        {
            VersionListView.SelectAll();
        }
    }

    public bool InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
        {
            if (VersionListView.IsAnySelected)
            {
                int index = Utils.FindSuitablePlacementIndex(VersionListView);

                if ((index >= 0) && (index < VersionListView.Items.Count))
                {
                    VersionListView.ScrollIntoView(VersionListView.Items[index]);

                    if (VersionListView.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        lvi.ContextFlyout.ShowAt(lvi);
                    }
                }
            }

            return true;
        }
        else if (VersionListView.ContainerFromIndex(((ItemsStackPanel)VersionListView.ItemsPanelRoot).FirstVisibleIndex) is ListViewItem lvi)
        {
            // treat as global hot keys
            // using the first visible index ensures that it's container exists, none of the menu commands are item specific
            return Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)lvi.ContextFlyout).Items, modifiers, key);
        }

        return false;
    }

    private void VersionListView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(null);

        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            object? versionRecord = (e.OriginalSource as FrameworkElement)?.DataContext;

            if (VersionListView.ContainerFromItem(versionRecord) is ListViewItem lvi)
            {
                if (!lvi.IsSelected)
                {
                    VersionListView.DeselectAll();
                    lvi.IsSelected = true;
                }
            }
        }
    }
}
