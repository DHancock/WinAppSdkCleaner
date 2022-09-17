using WinAppSdkCleaner.ViewModels;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for SdkView.xaml
/// </summary>
public partial class SdkViewBase : UserControl
{
    private readonly ViewCommand rescanCommand;
    private readonly ViewCommand removeCommand;
    private readonly ViewCommand copyCommand;
    
    private SdkViewModelBase? viewModel;
    private bool isIdle = true;

    public SdkViewBase()
    {
        InitializeComponent();

        rescanCommand = InitialiseCommand("Rescan", ExecuteRescan, CanRescan);
        removeCommand = InitialiseCommand("Remove", ExecuteRemove, CanRemove);
        copyCommand = InitialiseCommand("Copy", ExecuteCopy, CanCopy);

        DataContextChanged += (s, e) => viewModel = e.NewValue as SdkViewModelBase;

        Loaded += (s, a) =>
        {
            Debug.Assert(viewModel is not null);

            AdjustCommandsState();

            if (CanRescan())
                ExecuteRescan();
        };
    }

    private ViewCommand InitialiseCommand(string key, Action<object?> execute, Func<object?, bool> canExecute)
    {
        ViewCommand command = (ViewCommand)FindResource(key);
        command.CanExecuteProc = canExecute;
        command.ExecuteProc = execute;
        return command;
    }

    public async void ExecuteRescan(object? param = null)
    {
        try
        {
            IsIdle = false;
            await viewModel!.ExecuteRescan();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsIdle = true;
        }
    }

    private bool CanRescan(object? param = null) => IsIdle && viewModel!.CanRescan();

    private async void ExecuteRemove(object? param)
    {
        try
        {
            IsIdle = false;
            await viewModel!.ExecuteRemove();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            await viewModel!.ExecuteRescan();
            IsIdle = true;
        }
    }

    private bool CanRemove(object? param) => IsIdle && viewModel!.CanRemove();

    private void ExecuteCopy(object? param) => viewModel!.ExecuteCopy();

    private bool CanCopy(object? param) => IsIdle && viewModel!.CanCopy();

    private bool IsIdle
    {
        get => isIdle;

        set
        {
            isIdle = value;
            BusyIndicator.IsIndeterminate = !value;
            AdjustCommandsState();
        }
    }

    private void AdjustCommandsState()
    {
        rescanCommand.RaiseCanExecuteChanged();
        removeCommand.RaiseCanExecuteChanged();
        copyCommand.RaiseCanExecuteChanged();
    }

    private void SelectedTreeViewItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        AdjustCommandsState();
    }
}