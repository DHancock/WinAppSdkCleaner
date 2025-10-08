using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string message, string details) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) =>
        {
            Utils.PlayExclamation();
            Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";
        };
    }
}
