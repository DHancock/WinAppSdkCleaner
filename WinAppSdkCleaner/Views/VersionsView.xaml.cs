using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
public partial class VersionsView : UserControl
{
    public VersionsView()
    {
        InitializeComponent();

        DataContext = new VersionsViewModel();

        Loaded += async (s, e) =>
        {
            await ((VersionsViewModel)DataContext).LoadVersionInfo();
        };
    }
}


