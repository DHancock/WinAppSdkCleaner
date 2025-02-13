using WinAppSdkCleaner.Utils;

namespace WinAppSdkCleaner.Views;

internal sealed partial class CustomTitleBar : UserControl
{
    private AppWindow? parentAppWindow;

    public CustomTitleBar()
    {
        this.InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SizeChanged += (s, e) =>
            {
                if (IsLoaded)
                {
                    UpdateTitleBarPadding(e.NewSize.Width);
                }
            };

            ActualThemeChanged += (s, e) =>
            {
                UpdateThemeAndTransparency(s.ActualTheme);
            };

            Loaded += (s, e) =>
            {
                windowIcon.Foreground = new SolidColorBrush (IntegrityLevel.IsElevated ? Colors.Red : Colors.Green);
                UpdateTitleBarPadding(ActualWidth);
            };
        }
    }

    public AppWindow? ParentAppWindow
    {
        get => parentAppWindow;
        set
        {
            Debug.Assert(value is not null);
            parentAppWindow = value;
        }
    }

    public string Title
    {
        get => windowTitle.Text;
        set => windowTitle.Text = value ?? string.Empty;
    }

    private void UpdateTitleBarPadding(double width)
    {
        Debug.Assert(ParentAppWindow is not null);
        Debug.Assert(IsLoaded);

        double scaleFactor = this.XamlRoot.RasterizationScale;

        LeftPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.LeftInset / scaleFactor);
        RightPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.RightInset / scaleFactor);

        windowTitle.Width = Math.Max(width - (LeftPaddingColumn.Width.Value + IconColumn.Width.Value + RightPaddingColumn.Width.Value), 0);
    }

    public void UpdateThemeAndTransparency(ElementTheme theme)
    {
        Debug.Assert(ParentAppWindow is not null);
        Debug.Assert(ParentAppWindow.TitleBar is not null);

        if (theme == ElementTheme.Default)
        {
            theme = App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        }

        AppWindowTitleBar titleBar = ParentAppWindow.TitleBar;

        titleBar.BackgroundColor = Colors.Transparent;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (theme == ElementTheme.Light)
        {
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
        }
        else
        {
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Colors.DimGray;
            titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
        }
    }

    public void ParentWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            bool stateFound = VisualStateManager.GoToState(this, "Deactivated", false);
            Debug.Assert(stateFound);
        }
        else
        {
            bool stateFound = VisualStateManager.GoToState(this, "Activated", false);
            Debug.Assert(stateFound);
        }
    }

    public UIElement WindowIconArea => windowIconArea;
}
