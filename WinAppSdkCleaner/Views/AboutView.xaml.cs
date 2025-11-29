using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
internal sealed partial class AboutView : Page, IPageItem
{
    private readonly Version? version = typeof(App).Assembly.GetName().Version;
    private bool teachingTipDismissed = false;

    public AboutView()
    {
        InitializeComponent();

        VersionTextBlock.Text = $"Version: {version?.ToString(3)}";

        Loaded += AboutView_Loaded;
    }

    private void AboutView_Loaded(object sender, RoutedEventArgs e)
    {
        if ((version is not null) && !teachingTipDismissed)
        {
            // it's quite unlikely but possible that the latest version hasn't been loaded at this point
            UpdateInfo.IsOpen = version < App.Instance.LastestVersion;
        }
    }

    private void UpdateInfo_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
    {
        teachingTipDismissed = args.Reason == TeachingTipCloseReason.CloseButton;
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(HyperlinkTextBlock);
    }
}
