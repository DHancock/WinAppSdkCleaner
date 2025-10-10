namespace WinAppSdkCleaner.Views;

internal sealed class ContentDialogHelper
{
    private readonly MainWindow parentWindow;
    private ContentDialog? currentDialog = null;

    public ContentDialogHelper(MainWindow window)
    {
        parentWindow = window;
    }

    public async Task ShowErrorDialogAsync(string message, string details)
    {
        await ShowDialogAsync(new ErrorDialog(message, details));
    }

    public async Task<ContentDialogResult> ShowConfirmDialogAsync(string message)
    {
        return await ShowDialogAsync(new ConfirmDialog(message));
    }
    
    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (currentDialog is not null)
        {
            // while this may not be currently possible, it shouldn't be a fatal error either.
            Debug.Fail("canceling request for a second content dialog");
            return ContentDialogResult.None;
        }

        currentDialog = dialog;
        currentDialog.Opened += CurrentDialog_Opened;
        currentDialog.Closed += ContentDialog_Closed;

        currentDialog.Style = (Style)Application.Current.Resources["CustomContentDialogStyle"];
        currentDialog.XamlRoot = parentWindow.Content.XamlRoot;
        currentDialog.RequestedTheme = ((FrameworkElement)parentWindow.Content).ActualTheme;
        currentDialog.FlowDirection = ((FrameworkElement)parentWindow.Content).FlowDirection;

        return await currentDialog.ShowAsync();
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        currentDialog = null;
        parentWindow.ContentDialogClosed();
    }

    private void CurrentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        parentWindow.ContentDialogOpened();
    }

    public bool IsContentDialogOpen => currentDialog is not null;
}
