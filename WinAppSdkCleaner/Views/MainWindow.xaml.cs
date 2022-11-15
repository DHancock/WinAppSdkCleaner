using WinAppSdkCleaner.Models;
using CsWin32Lib;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new ViewModels.MainWindowViewModel();

        SourceInitialized += (s, a) =>
        {
            if (Settings.Data.IsFirstRun)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                Rect bounds = ValidateRestoreBounds(Settings.Data.RestoreBounds);

                Left = bounds.TopLeft.X;
                Top = bounds.TopLeft.Y;

                Width = bounds.Width;
                Height = bounds.Height;

                if (Settings.Data.WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = Settings.Data.WindowState;
            }
        };

        Closing += (s, e) =>
        {
            Settings.Data.WindowState = WindowState;
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.Save();
        };

        Activated += (s, e) =>
        {
            if (sdkView.CanSearch())
                sdkView.ExecuteSearch();
        };
    }

    internal Rect ValidateRestoreBounds(Rect restoreBounds)
    {
        try
        {
            PresentationSource? source = PresentationSource.FromVisual(this);

            if (source is not null)
            {
                Rect windowArea = restoreBounds;

                // convert to device pixels
                windowArea.Transform(source.CompositionTarget.TransformToDevice);

                Rect workingArea = Monitors.GetWorkingAreaOfClosestMonitor(windowArea);

                Point topLeft = windowArea.TopLeft;

                if ((topLeft.Y + windowArea.Height) > workingArea.Bottom)
                    topLeft.Y = workingArea.Bottom - windowArea.Height;

                if (topLeft.Y < workingArea.Top)
                    topLeft.Y = workingArea.Top;

                if ((topLeft.X + windowArea.Width) > workingArea.Right)
                    topLeft.X = workingArea.Right - windowArea.Width;

                if (topLeft.X < workingArea.Left)
                    topLeft.X = workingArea.Left;

                Point bottomRight = new Point(topLeft.X + Math.Min(windowArea.Width, workingArea.Width), 
                                                topLeft.Y + Math.Min(windowArea.Height, workingArea.Height));

                windowArea = new Rect(topLeft, bottomRight);

                // convert back to wpf coordinates
                windowArea.Transform(source.CompositionTarget.TransformFromDevice);

                return windowArea;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return restoreBounds;
    }
}




