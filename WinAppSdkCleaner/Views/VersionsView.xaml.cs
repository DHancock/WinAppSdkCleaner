using WinAppSdkCleaner.Utilities;
using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
internal sealed partial class VersionsView : Page, IPageItem
{
    private readonly VersionsViewModel viewModel;

    public VersionsView()
    {
        InitializeComponent();

        viewModel = new VersionsViewModel(this.DispatcherQueue);
        Loaded += VersionsView_Loaded;
    }

    private void VersionsView_Loaded(object sender, RoutedEventArgs e)
    {
        // allow keyboard interaction without the need to tab into the list
        VersionListView.Focus(FocusState.Programmatic);
    }

    internal VersionsViewModel ViewModel => viewModel;

    public int PassthroughCount => 1;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(VersionListView);
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = VersionListView.SelectedItems.Count > 0;
    }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
    }

    private void Page_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if ((VersionListView.SelectedItems.Count > 0) && (args.Key == VirtualKey.C) && args.Modifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            args.Handled = true;
            VersionsViewModel.ExecuteCopy(VersionListView.SelectedItems);
        }
    }
}
