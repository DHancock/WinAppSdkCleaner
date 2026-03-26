using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string message, string details) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";

        Utils.PlayExclamation();
    }
}
