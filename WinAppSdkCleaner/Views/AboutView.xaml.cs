namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();

        AssemblyName name = typeof(App).Assembly.GetName()!;

        NameTextBlock.Text = name.Name;
        VersionTextBlock.Text = name.Version!.ToString();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = e.Uri.AbsoluteUri;
        psi.UseShellExecute = true;

        Process.Start(psi);
    }
}
