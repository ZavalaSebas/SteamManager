using System.Windows;
using System.Windows.Controls;
using SteamManager.Dialogs;
using SteamManager.ViewModels;

namespace SteamManager.Views;

public partial class GameManagerView : UserControl
{
    private GameManagerViewModel? _viewModel;

    public GameManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SmartUnlockProgressOverlay.CancelRequested += OnProgressOverlayCancelRequested;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = e.NewValue as GameManagerViewModel;
    }

    private void OnProgressOverlayCancelRequested(object? sender, EventArgs e)
    {
        _viewModel?.CancelSmartUnlock();
    }

    private void UnlockDropdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    private async void SmartUnlockMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameManagerViewModel vm)
            return;

        var dialog = new SmartUnlockDialog();
        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        await vm.ExecuteSmartUnlockAsync(dialog.MinDelay * 1000, dialog.MaxDelay * 1000, dialog.ShowOverlay);

        if (vm.SmartUnlockAppliedCount > 0 || vm.SmartUnlockProtectedCount > 0 || vm.SmartUnlockFailedCount > 0 || vm.SmartUnlockWasCancelled)
        {
            var resultDialog = new SmartUnlockResultDialog(
                vm.SmartUnlockAppliedCount,
                vm.SmartUnlockProtectedCount,
                vm.SmartUnlockFailedCount,
                vm.SmartUnlockWasCancelled);
            resultDialog.ShowDialog();
        }
    }
}
