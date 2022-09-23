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
    }

    internal Rect ValidateRestoreBounds(Rect restoreBounds)
    {
        try
        {
            PresentationSource? source = PresentationSource.FromVisual(this);

            if (source is not null)
            {
                Point[] pIn = new Point[] { new Point(restoreBounds.Left, restoreBounds.Top), new Point(restoreBounds.Right, restoreBounds.Bottom) };

                // convert to device pixels
                source.CompositionTarget.TransformToDevice.Transform(pIn);

                Rect windowArea = new Rect(pIn[0], pIn[1]);
                Rect workingArea = Monitors.GetWorkingAreaOfClosestMonitor(windowArea);

                Point topLeft = pIn[0];

                if ((topLeft.Y + windowArea.Height) > workingArea.Bottom)
                    topLeft.Y = workingArea.Bottom - windowArea.Height;

                if (topLeft.Y < workingArea.Top)
                    topLeft.Y = workingArea.Top;

                if ((topLeft.X + windowArea.Width) > workingArea.Right)
                    topLeft.X = workingArea.Right - windowArea.Width;

                if (topLeft.X < workingArea.Left)
                    topLeft.X = workingArea.Left;

                Point[] pOut = new Point[] { topLeft, new Point(topLeft.X + windowArea.Width, topLeft.Y + windowArea.Height) };

                // convert back to wpf coordinates in dip's
                source.CompositionTarget.TransformFromDevice.Transform(pOut);

                return new Rect(pOut[0], pOut[1]);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }

        return restoreBounds;
    }
}




