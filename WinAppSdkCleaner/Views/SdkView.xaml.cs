using WinAppSdkCleaner.ViewModels;
using WinAppSdkCleaner.Utilites;

namespace WinAppSdkCleaner.Views;

/// <summary>
/// Interaction logic for SdkView.xaml
/// </summary>
internal sealed partial class SdkView : Page, IPageItem
{
    private RelayCommand SearchCommand { get; }
    private RelayCommand RemoveCommand { get; }

    private bool isIdle = true;
    private DateTime lastPointerTimeStamp;
    private readonly SdkViewModel viewModel;

    public SdkView()
    {
        InitializeComponent();

        SearchCommand = new RelayCommand(ExecuteSearch, CanSearch);
        RemoveCommand = new RelayCommand(ExecuteRemove, CanRemove);

        viewModel = new SdkViewModel();
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SdkList))
        {
            if (SdkTreeView.IsLoaded)
            {
                UpdateTree();
                UpdateSortButtonIsEnabled();
            }
            else
            {
                // a not very elegant way of ensuring there's only one update queued
                SdkTreeView.Loaded -= SdkTreeView_Loaded;
                SdkTreeView.Loaded += SdkTreeView_Loaded;
            }
        }

        void SdkTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            SdkTreeView.Loaded -= SdkTreeView_Loaded;

            UpdateTree();
            UpdateSortButtonIsEnabled();
        }
    }

    private void UpdateTree()
    {
        UpdateTree(SdkTreeView.RootNodes, viewModel.SdkList, 0);

        void UpdateTree(IList<TreeViewNode> nodes, List<ItemBase> newData, int depth)
        {
            int nodeIndex = 0;

            while (nodeIndex < nodes.Count)
            {
                TreeViewNode node = nodes[nodeIndex];

                int result = newData.BinarySearch((ItemBase)node.Content);

                if (result < 0)
                {
                    nodes.RemoveAt(nodeIndex);
                }
                else
                {
                    int newNodeCount = result - nodeIndex;

                    while (newNodeCount-- > 0)
                    {
                        TreeViewNode newNode = CreateTree(newData[nodeIndex + newNodeCount]);
                        newNode.IsExpanded = depth == 0;
                        nodes.Insert(nodeIndex, newNode);
                    }

                    nodeIndex = result;
                    int previousCount = ((ItemBase)node.Content).OtherAppsCount;

                    node.Content = newData[nodeIndex];

                    if (previousCount != newData[nodeIndex].OtherAppsCount)
                    {
                        UpdateDependentAppsCount(node);
                    }

                    if ((newData[nodeIndex].Children.Count + node.Children.Count) > 0)
                    {
                        UpdateTree(node.Children, newData[nodeIndex].Children, depth + 1);
                    }

                    nodeIndex += 1;
                }
            }

            while (nodeIndex < newData.Count)
            {
                TreeViewNode newNode = CreateTree(newData[nodeIndex++]);
                newNode.IsExpanded = depth == 0;
                nodes.Add(newNode);
            }
        }

        static TreeViewNode CreateTree(ItemBase item)
        {
            TreeViewNode node = new() { Content = item };

            foreach (ItemBase child in item.Children)
            {
                TreeViewNode childNode = CreateTree(child);
                node.Children.Add(childNode);
            }

            return node;
        }

        void UpdateDependentAppsCount(TreeViewNode node)
        {
            TreeViewItem? tvi = (TreeViewItem)SdkTreeView.ContainerFromNode(node);

            if (tvi is not null)
            {
                TextBlock? tb = tvi.FindChild<TextBlock>("OtherAppsCountTextBox");

                if (tb is not null)
                {
                    tb.Text = ((ItemBase)node.Content).OtherAppsCountStr;
                }
            }
        }
    }

    private void UpdateSortButtonIsEnabled()
    {
        SortButton.IsEnabled = viewModel.SdkList.Count > 0;
    }

    public async void ExecuteSearch(object? param = null)
    {
        try
        {
            IsIdle = false;
            await viewModel.ExecuteSearch();
            IsIdle = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
            IsIdle = true;
            await App.MainWindow.ContentDialogHelper.ShowErrorDialogAsync("An error occurred while searching.", ex.ToString());
        }
    }

    public bool CanSearch(object? param = null) => IsIdle;

    private async void ExecuteRemove(object? param)
    {
        if (SdkTreeView.SelectedNode?.Content is SdkItem sdk)
        {
            try
            {
                string message;

                if (sdk.OtherAppsCount > 0)
                {
                    message = $"{sdk.HeadingText} has dependent applications.{Environment.NewLine}Are you sure that you want to remove it?";
                }
                else
                {
                    message = $"Are you sure that you want to remove {sdk.HeadingText}?";
                }

                ContentDialogResult result = await App.MainWindow.ContentDialogHelper.ShowConfirmDialogAsync(message);

                if (result == ContentDialogResult.Primary)
                {
                    IsIdle = false;
                    await viewModel.ExecuteRemove(sdk);
                    await viewModel.ExecuteSearch();
                    IsIdle = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());

                string message;
                string details;

                if (ex is TimeoutException)
                {
                    message = $"Removing {sdk.HeadingText} timed out.";
                    details = "This can occur if an unpackaged, framework dependent application that is depending on this SDK is currently executing.";
                }
                else
                {
                    message = $"An error occurred removing {sdk.HeadingText}.";
                    details = ex.ToString();
                }

                ExecuteSearch();
                await App.MainWindow.ContentDialogHelper.ShowErrorDialogAsync(message, details);
            }
        }
    }

    private bool CanRemove(object? param)
    {
        bool canRemove = IsIdle && (SdkTreeView.SelectedNode?.Content is SdkItem);
        RemoveIcon.Opacity = canRemove ? 1.0 : 0.4;
        return canRemove;
    }

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
        SearchCommand.RaiseCanExecuteChanged();
        RemoveCommand.RaiseCanExecuteChanged();
    }

    private void SelectedTreeViewItemChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        AdjustCommandsState();
    }

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        SdkViewModel.ExecuteCopy((ItemBase)((TreeViewNode)((FrameworkElement)sender).DataContext).Content);
    }

    private void SdkTreeView_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (IsControlKeyDown() && (e.Key == VirtualKey.C) && (SdkTreeView.SelectedItem is TreeViewNode node))
        {
            SdkViewModel.ExecuteCopy((ItemBase)node.Content);
        }

        static bool IsKeyDown(VirtualKey key)
        {
            return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
        }

        static bool IsControlKeyDown()
        {
            return IsKeyDown(VirtualKey.LeftControl) || IsKeyDown(VirtualKey.RightControl) || IsKeyDown(VirtualKey.Control);
        }
    }

    private void SdkTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        TimeSpan doubleClickTime = TimeSpan.FromMilliseconds(PInvoke.GetDoubleClickTime());
        DateTime utcNow = DateTime.UtcNow;

        if ((utcNow - lastPointerTimeStamp) < doubleClickTime)
        {
            TreeViewNode tvn = (TreeViewNode)args.InvokedItem;
            tvn.IsExpanded = !tvn.IsExpanded;
        }
        else
        {
            lastPointerTimeStamp = utcNow;
        }
    }

    public int PassthroughCount => 4;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        rects[0] = Utils.GetPassthroughRect(SdkTreeView);
        rects[1] = Utils.GetPassthroughRect(RefreshButton);
        rects[2] = Utils.GetPassthroughRect(RemoveButton);
        rects[3] = Utils.GetPassthroughRect(SortButton);
    }

    private void SortButton_Click(object sender, RoutedEventArgs e)
    {
        Settings.Instance.SortAscending = !Settings.Instance.SortAscending;

        List<TreeViewNode> nodes = new(SdkTreeView.RootNodes);
        nodes.Reverse();

        TreeViewNode? selectedNode = SdkTreeView.SelectedNode;

        SdkTreeView.RootNodes.Clear();

        foreach (TreeViewNode node in nodes)
        {
            SdkTreeView.RootNodes.Add(node);
        }

        if (selectedNode is not null)
        {
            SdkTreeView.SelectedNode = selectedNode;
        }
    }
}