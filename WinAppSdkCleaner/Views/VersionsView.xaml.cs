using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
internal sealed partial class VersionsView : Page
{
    private VersionsViewModel? viewModel;
    private readonly CollectionViewSource viewSource = new() { IsSourceGrouped = true };

    public VersionsView()
    {
        InitializeComponent();

        // this view may not have been created when the model notifies that data is available
        VersionListView.Loaded += (s, e) => UpdateCollectionViewSource();
    }

    internal VersionsViewModel? ViewModel
    {
        get => viewModel;

        set
        {
            Debug.Assert(viewModel is null);
            Debug.Assert(value is not null); 
            
            viewModel = value;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }


    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VersionsViewModel.VersionsList))
        {
            // the model loads data on a non ui thread
            DispatcherQueue.TryEnqueue(VersionListViewPropertyChanged);
        }
    }

    private void VersionListViewPropertyChanged()
    {
        if (VersionListView.IsLoaded)
        {
            UpdateCollectionViewSource();
        }
        else
        {
            VersionListView.Loaded += VersionListView_Loaded;
        }

        void VersionListView_Loaded(object sender, RoutedEventArgs e)
        {
            VersionListView.Loaded -= VersionListView_Loaded;
            UpdateCollectionViewSource();
        }
    }

    private void UpdateCollectionViewSource()
    {
        if (VersionListView.Items.Count == 0)
        {
            viewSource.Source = viewModel?.VersionsList;
            VersionListView.ItemsSource = viewSource.View;
        }
    }

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
}
