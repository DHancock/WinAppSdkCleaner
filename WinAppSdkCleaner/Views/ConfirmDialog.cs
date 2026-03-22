namespace WinAppSdkCleaner.Views;

internal sealed partial class ConfirmDialog : ContentDialog
{
    public ConfirmDialog(string message) : base()
    {
        DefaultButton = ContentDialogButton.Primary;

        PrimaryButtonText = "OK";
        SecondaryButtonText = "Cancel";

        Content = message;
    }
}
