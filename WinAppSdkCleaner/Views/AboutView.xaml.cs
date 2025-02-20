namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
internal sealed partial class AboutView : Page
{
    public AboutView()
    {
        InitializeComponent();

        AssemblyName assemblyName = typeof(App).Assembly.GetName();

        NameTextBlock.Text = assemblyName.Name;
        VersionTextBlock.Text = assemblyName.Version?.ToString(3);

        // Use the Tag to identify that this text block contains a hyperlink
        HyperlinkTextBlock.Tag = HyperlinkTextBlock;
    }                    
}
