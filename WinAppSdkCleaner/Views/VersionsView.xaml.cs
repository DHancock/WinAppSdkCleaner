using Microsoft.UI.Xaml.Data;

using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
public partial class VersionsView : Page
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

    private static void ExecuteCopy(IList<object> selectedItems)
    {
        StringBuilder sb = new StringBuilder();

        foreach (object item in selectedItems)
        {
            if (item is DisplayVersion displayVersion)
            {
                sb.AppendLine($"{displayVersion.SemanticVersion}\t{displayVersion.PackageVersion}");
            }
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
            ExecuteCopy(new List<object>(1) { version });
        }
    }
}
