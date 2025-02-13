namespace WinAppSdkCleaner.Views;

internal sealed partial class ConfirmDialog : ContentDialog
{
    public ConfirmDialog(string message) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        SecondaryButtonText = "Cancel";

        Loaded += (s, e) =>
        {
            Content = message;
        };
    }
}
