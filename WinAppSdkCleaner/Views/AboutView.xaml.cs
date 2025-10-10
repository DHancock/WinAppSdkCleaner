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

        AssemblyName assemblyName = typeof(App).Assembly.GetName();

        NameTextBlock.Text = assemblyName.Name;
        VersionTextBlock.Text = assemblyName.Version?.ToString(3);
    }

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(HyperlinkTextBlock);
    }
}
