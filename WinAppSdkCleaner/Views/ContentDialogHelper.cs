﻿using WinAppSdkCleaner.Utils;

namespace WinAppSdkCleaner.Views;

internal class ContentDialogHelper
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
        currentDialog.Closing += ContentDialog_Closing;
        currentDialog.Closed += ContentDialog_Closed;
        currentDialog.Loaded += CurrentDialog_Loaded;

        currentDialog.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
        currentDialog.Title = App.cAppDisplayName;
        currentDialog.XamlRoot = parentWindow.Content.XamlRoot;
        currentDialog.RequestedTheme = ((FrameworkElement)parentWindow.Content).ActualTheme;
        currentDialog.FlowDirection = ((FrameworkElement)parentWindow.Content).FlowDirection;

        EnableCaptionButtons(enable: false);

        return await currentDialog.ShowAsync();
    }

    private void EnableCaptionButtons(bool enable)
    {
        HWND hWnd = PInvoke.FindWindowEx(parentWindow.WindowHandle, HWND.Null, "InputNonClientPointerSource", null);
        Debug.Assert(!hWnd.IsNull);

        if (!hWnd.IsNull)
        {
            PInvoke.EnableWindow(hWnd, enable);
        }
    }

    private static void CurrentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ContentControl? contentControl = ((ContentDialog)sender).FindChild<ContentControl>("Title");

        if (contentControl is not null)
        {                    
            // no lightweight styling, and size 20 is a bit loud
            contentControl.FontSize = 18;
        }
    }

    private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        Debug.WriteLine("ContentDialog_Closing");

        EnableCaptionButtons(enable: true);
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        Debug.WriteLine("ContentDialog_Closed");

        currentDialog = null;
    }
                                                       
    public bool IsContentDialogOpen => currentDialog is not null;
}
