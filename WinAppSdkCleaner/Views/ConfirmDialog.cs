namespace WinAppSdkCleaner.Views;

internal sealed partial class ConfirmDialog : ContentDialog
{
    public ConfirmDialog(string message) : base()
    {
        DefaultButton = ContentDialogButton.Primary;

        PrimaryButtonText = "Yes";
        SecondaryButtonText = "Cancel";

        Content = message;
    }
}
