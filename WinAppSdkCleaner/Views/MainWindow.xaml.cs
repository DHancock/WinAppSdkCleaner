using WinAppSdkCleaner.ViewModels;
using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : Window
{
    private DateTime lastPointerTimeStamp;
    private SdkView? sdkViewPage;

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };

    public MainWindow(string title) : this()
    {
        AssemblyName assemblyName = typeof(App).Assembly.GetName();
        Trace.WriteLine($"{assemblyName.Name} version: {assemblyName.Version?.ToString(3)}");
        Trace.WriteLine($"All users: {IntegrityLevel.IsElevated}");

        AppWindow.Closing += (s, a) =>
        {
            SaveSettings();
        };

        customTitleBar.ParentAppWindow = AppWindow;
        customTitleBar.UpdateThemeAndTransparency(LayoutRoot.ActualTheme);
        customTitleBar.Title = title;
        customTitleBar.WindowIconArea.PointerPressed += WindowIconArea_PointerPressed;
        Activated += customTitleBar.ParentWindow_Activated;

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        // always set the window icon and title, it's used in the task switcher
        AppWindow.SetIcon("Resources\\app.ico");
        AppWindow.Title = title;

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
        {
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }

        AppWindow.MoveAndResize(ValidateRestoreBounds(Settings.Instance.RestoreBounds));

        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if ((args.WindowActivationState != WindowActivationState.Deactivated) && 
            (sdkViewPage is not null) && 
            sdkViewPage.CanSearch() &&
            !ContentDialogHelper.IsContentDialogOpen)
        {
            sdkViewPage.ExecuteSearch();
        }
    }

    public static string SdkTabTitle => IntegrityLevel.IsElevated ? "All Users" : "User Scope";

    private RectInt32 ValidateRestoreBounds(RectInt32 windowArea)
    {
        if (windowArea == default)
        {
            return CenterInPrimaryDisplay();
        }

        RectInt32 workArea = DisplayArea.GetFromRect(windowArea, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = new PointInt32(windowArea.X, windowArea.Y);

        if ((position.Y + windowArea.Height) > (workArea.Y + workArea.Height))
        {
            position.Y = (workArea.Y + workArea.Height) - windowArea.Height;
        }

        if (position.Y < workArea.Y)
        {
            position.Y = workArea.Y;
        }

        if ((position.X + windowArea.Width) > (workArea.X + workArea.Width))
        {
            position.X = (workArea.X + workArea.Width) - windowArea.Width;
        }

        if (position.X < workArea.X)
        {
            position.X = workArea.X;
        }

        SizeInt32 size = new SizeInt32(Math.Min(windowArea.Width, workArea.Width),
                                        Math.Min(windowArea.Height, workArea.Height));

        return new RectInt32(position.X, position.Y, size.Width, size.Height);
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            Type? type;

            switch (item.Tag as string) // have to explicitly specify the full type name to allow for trimming
            {
                case "SdkView": type = Type.GetType("WinAppSdkCleaner.Views.SdkView"); break;
                case "TraceView": type = Type.GetType("WinAppSdkCleaner.Views.TraceView"); break;
                case "VersionsView": type = Type.GetType("WinAppSdkCleaner.Views.VersionsView"); break;
                case "AboutView": type = Type.GetType("WinAppSdkCleaner.Views.AboutView"); break;
                default:
                    throw new InvalidOperationException();
            }

            if (type is not null)
            {
                ContentFrame.NavigateToType(type, null, frameNavigationOptions);
            }
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        FrameworkElement page = (FrameworkElement)e.Content;

        if (page.Tag is null)
        {
            page.Tag = page;

            if (page is SdkView view)
            {
                // to refresh data on window activations
                sdkViewPage = view;
            }

            page.Loaded += Page_Loaded;
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetWindowDragRegions();
        }
    }

    private void ContentFrame_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetWindowDragRegions();
    }

    private RectInt32 CenterInPrimaryDisplay()
    {
        RectInt32 workArea = DisplayArea.Primary.WorkArea;
        RectInt32 windowArea;

        windowArea.Width = ConvertToDeviceSize(cInitialWidth);
        windowArea.Height = ConvertToDeviceSize(cInitialHeight);

        windowArea.Width = Math.Min(windowArea.Width, workArea.Width);
        windowArea.Height = Math.Min(windowArea.Height, workArea.Height);

        windowArea.Y = (workArea.Height - windowArea.Height) / 2;
        windowArea.X = (workArea.Width - windowArea.Width) / 2;

        // guarantee title bar is visible, the minimum window size may trump working area
        windowArea.Y = Math.Max(windowArea.Y, workArea.Y);
        windowArea.X = Math.Max(windowArea.X, workArea.X);

        return windowArea;
    }

    private void WindowIconArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ShowSystemMenu(viaKeyboard: true); // open at keyboard location as not to obscure double clicks

        TimeSpan doubleClickTime = TimeSpan.FromMilliseconds(PInvoke.GetDoubleClickTime());
        DateTime utcNow = DateTime.UtcNow;

        if ((utcNow - lastPointerTimeStamp) < doubleClickTime)
        {
            PostCloseMessage();
        }
        else
        {
            lastPointerTimeStamp = utcNow;
        }
    }

    private void LayoutRoot_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            PostCloseMessage();
            e.Handled = true;
        }
    }

    private void SaveSettings()
    {
        Settings.Instance.RestoreBounds = RestoreBounds;
        Settings.Instance.Save();
    }
}




