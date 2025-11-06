using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
internal sealed partial class AboutView : Page, IPageItem
{
    public AboutView()
    {
        InitializeComponent();

        VersionTextBlock.Text = $"Version: {typeof(App).Assembly.GetName().Version?.ToString(3)}";
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(HyperlinkTextBlock);
    }
}
