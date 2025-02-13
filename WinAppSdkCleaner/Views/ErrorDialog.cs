using WinAppSdkCleaner.Utils;

namespace WinAppSdkCleaner.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string message, string details) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) =>
        {
            User32Sound.PlayExclamation();
            Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";
        };
    }
}
