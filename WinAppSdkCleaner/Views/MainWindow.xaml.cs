using WinAppSdkCleaner.Models;
using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += (s, a) =>
        {
            if (Settings.Data.IsFirstRun)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                Rect bounds = Utils.ValidateRestoreBounds(Settings.Data.RestoreBounds);

                Left = bounds.TopLeft.X;
                Top = bounds.TopLeft.Y;

                Width = bounds.Width;
                Height = bounds.Height;

                WindowState = Settings.Data.WindowState;
            }
        };

        Closing += (s, e) =>
        {
            Settings.Data.WindowState = WindowState;
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.Save();
        };
    }
}




