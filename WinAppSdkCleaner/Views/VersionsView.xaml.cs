using WinAppSdkCleaner.ViewModels;
using static WinAppSdkCleaner.ViewModels.VersionsViewModel;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for VersionsView.xaml
/// </summary>
public partial class VersionsView : UserControl
{
    private readonly ViewCommand winAppSdkCopyCommand;
    private readonly ViewCommand reunionCopyCommand;

    public VersionsView()
    {
        InitializeComponent();

        DataContext = new VersionsViewModel();

        winAppSdkCopyCommand = InitialiseCommand("WinAppSdkCopy", ExecuteWinAppSdkCopy, CanWinAppSdkCopy);
        reunionCopyCommand = InitialiseCommand("ReunionCopy", ExecuteReunionCopy, CanReunionCopy);

        Loaded += async (s, e) =>
        {
            await ((VersionsViewModel)DataContext).LoadVersionInfo();
        };
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    private void ExecuteWinAppSdkCopy(object? param) => ExecuteCopy(WinAppSdkListView);
    private bool CanWinAppSdkCopy(object? param) => CanCopy(WinAppSdkListView);

    private void ExecuteReunionCopy(object? param) => ExecuteCopy(ReunionListView);
    private bool CanReunionCopy(object? param) => CanCopy(ReunionListView);

    private void WinAppSdkSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        winAppSdkCopyCommand.RaiseCanExecuteChanged();
    }

    private void ReunionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        reunionCopyCommand.RaiseCanExecuteChanged();
    }

    private static void ExecuteCopy(ListView list)
    {
        StringBuilder sb = new StringBuilder();

        foreach (object item in list.SelectedItems)
        {
            Debug.Assert(item is DisplayVersion);

            if (item is DisplayVersion displayVersion)
                sb.AppendLine($"{displayVersion.SemanticVersion}\t{displayVersion.PackageVersion}");
        } 

        if (sb.Length > 0)
            Clipboard.SetText(sb.ToString());
    }

    private static bool CanCopy(ListView list) => list.SelectedItems.Count > 0;
}


