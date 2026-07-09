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

    private void GridContextCopy_Click(object sender, RoutedEventArgs e)
    {
        object? item = ((FrameworkElement)sender).DataContext;

        if (item is not null)
        {
            if (VersionListView.SelectedItems.Contains(item))
            {
                VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
            }
            else
            {
                VersionsViewModel.ExecuteCopy([item]);
            }
        }
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }

    private void ListViewContextCopy_Click(object sender, RoutedEventArgs e)
    {
        // called via Shift+F10 activation of the list view context menu
        VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
    }

    private void VersionListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if ((e.Key == VirtualKey.C) && (VersionListView.SelectedItems.Count > 0) && IsControlKeyDown())
        {
            VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
        }

        static bool IsControlKeyDown()
        {
            return IsKeyDown(VirtualKey.Control) || IsKeyDown(VirtualKey.LeftControl) || IsKeyDown(VirtualKey.RightControl);

            static bool IsKeyDown(VirtualKey key)
            {
                return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
            }
        }
    }
}
