using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Schaumamal.Models;
using Schaumamal.Models.Dumper;
using Schaumamal.ViewModels;
using System.ComponentModel;

namespace Schaumamal.Views;

public partial class ButtonLayer : UserControl
{
    public ButtonLayer()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AppViewModel vm)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AppViewModel vm) return;
        switch (e.PropertyName)
        {
            case nameof(AppViewModel.State):
                UpdateStateVisibility(vm);
                break;
            case nameof(AppViewModel.SelectedDump):
                CurrentDumpText.Text = $"{vm.SelectedDump.Nickname}";
                break;
            case nameof(AppViewModel.DumpProgress):
                DumpProgressBar.Value = vm.DumpProgress;
                ProgressText.Text = $"{vm.DumpProgressText} {(int)(vm.DumpProgress * 100)}%";
                break;
            case nameof(AppViewModel.DisplayIndex):
            case nameof(AppViewModel.DisplayCount):
                DisplayCounterText.Text = $"{vm.DisplayIndex + 1}/{vm.DisplayCount}";
                break;
        }
    }

    private void UpdateStateVisibility(AppViewModel vm)
    {
        var state = vm.State;
        SuggestionText.IsVisible = state == InspectorState.Empty;
        CurrentDumpPanel.IsVisible = state == InspectorState.Populated;
        ProgressPanel.IsVisible = state == InspectorState.Waiting;
        DisplayControlPill.IsVisible = state == InspectorState.Populated;
        ExtractButton.IsEnabled = state != InspectorState.Waiting;
        ExtractButton.Background = state != InspectorState.Waiting
            ? Avalonia.Media.Brushes.DarkRed : Avalonia.Media.Brushes.Gray;
    }

    private void OnExtractButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AppViewModel vm)
            _ = vm.ExtractAsync(new DumpProgressHandler((p, t) => { vm.DumpProgress = p; vm.DumpProgressText = t; }));
    }

    private void OnHistoryButtonClicked(object? sender, RoutedEventArgs e)
    {
        var mainView = this.FindAncestorOfType<MainView>();
        var fw = mainView?.FindDescendantOfType<FloatingWindowLayer>();
        fw?.Show();
    }

    private void OnPreviousDisplayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AppViewModel vm) vm.SwitchDisplay(Direction.Previous);
    }

    private void OnNextDisplayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AppViewModel vm) vm.SwitchDisplay(Direction.Next);
    }
}
