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

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        object item = ((FrameworkElement)sender).DataContext;

        if (VersionListView.SelectedItems.Contains(item))
        {
            VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
        }
        else
        {
            VersionsViewModel.ExecuteCopy([item]);
        }
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }
}
