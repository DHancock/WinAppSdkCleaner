namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();

        AssemblyName assemblyName = typeof(App).Assembly.GetName();

        NameTextBlock.Text = assemblyName.Name;
        
        if (assemblyName.Version is not null)
            VersionTextBlock.Text = assemblyName.Version.ToString();

        Trace.WriteLine($"{NameTextBlock.Text} - {VersionTextBlock.Text}");
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true,
        };

        Process.Start(psi);
    }
}
