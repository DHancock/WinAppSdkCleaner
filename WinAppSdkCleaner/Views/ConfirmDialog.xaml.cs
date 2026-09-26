namespace WinAppSdkCleaner.Views;

internal sealed partial class ConfirmDialog : ContentDialog
{
    private ConfirmDialog() : base()
    {
        this.InitializeComponent();
    }

    public ConfirmDialog(string message) : this()
    {
        DefaultButton = ContentDialogButton.Primary;

        PrimaryButtonText = "Yes";
        SecondaryButtonText = "Cancel";

        Loaded += (s, e) => ConfirmTextBlock.Text = message;
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.ShowConfirmDialog = ((CheckBox)sender).IsChecked is false;
    }
}
