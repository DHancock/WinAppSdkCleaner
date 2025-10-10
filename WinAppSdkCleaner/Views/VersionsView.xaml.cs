using WinAppSdkCleaner.Utilites;
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

    private void ExecuteCopy(IList<object> selectedItems)
    {
        // the selected items are in the time order that they were selected in
        // need to convert them back to sorted list ordering
        List<(int index, DisplayVersion displayVersion)> indexedList = new(selectedItems.Count);

        foreach (object item in selectedItems)
        {
            indexedList.Add((VersionListView.Items.IndexOf(item), (DisplayVersion)item));
        }

        StringBuilder sb = new StringBuilder();

        foreach ((int index, DisplayVersion displayVersion) in indexedList.OrderBy(x => x.index))
        {
            sb.AppendLine($"{displayVersion.SdkName} {displayVersion.SdkVersion}\t{displayVersion.PackageVersion}");
        }

        if (sb.Length > 0)
        {
            DataPackage dp = new DataPackage();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        DisplayVersion version = (DisplayVersion)((FrameworkElement)sender).DataContext;

        if (VersionListView.SelectedItems.Contains(version))
        {
            ExecuteCopy(VersionListView.SelectedItems);
        }
        else
        {
            ExecuteCopy([version]);
        }
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }
}
