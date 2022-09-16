using System.Runtime.InteropServices;

using WinAppSdkCleaner.Models;

using Windows.ApplicationModel.Activation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

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
        PresentationSource? source = PresentationSource.FromVisual(this);

        if (source is not null)
        {
            Point[] pIn = new Point[] { new Point(restoreBounds.Left, restoreBounds.Top), new Point(restoreBounds.Right, restoreBounds.Bottom) };

            // convert to device pixels
            source.CompositionTarget.TransformToDevice.Transform(pIn);

            RECT windowArea = ConvertToRECT(pIn[0], pIn[1]);

            HMONITOR hMonitor = PInvoke.MonitorFromRect(windowArea, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            if (hMonitor != 0)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

                if (PInvoke.GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    RECT workingArea = monitorInfo.rcWork;
                    Point topLeft = pIn[0];

                    if ((topLeft.Y + windowArea.Height) > workingArea.bottom)
                        topLeft.Y = workingArea.bottom - windowArea.Height;

                    if (topLeft.Y < workingArea.top)
                        topLeft.Y = workingArea.top;

                    if ((topLeft.X + windowArea.Width) > workingArea.right)
                        topLeft.X = workingArea.right - windowArea.Width;

                    if (topLeft.X < workingArea.left)
                        topLeft.X = workingArea.left;

                    Point[] pOut = new Point[] { topLeft, new Point(topLeft.X + windowArea.Width, topLeft.Y + windowArea.Height) };

                    // convert back to wpf coordinates in dip's
                    source.CompositionTarget.TransformFromDevice.Transform(pOut);

                    return new Rect(pOut[0], pOut[1]);
                }
            }
        }

        return restoreBounds;
    }

    private static RECT ConvertToRECT(Point topLeft, Point bottomRight)
    {
        RECT outRECT = new RECT();

        // avoids accumulating rounding errors
        outRECT.top = Convert.ToInt32(topLeft.Y);
        outRECT.left = Convert.ToInt32(topLeft.X);
        outRECT.bottom = outRECT.top + Convert.ToInt32(bottomRight.Y - topLeft.Y);
        outRECT.right = outRECT.left + Convert.ToInt32(bottomRight.X - topLeft.X);

        return outRECT;
    }
}




