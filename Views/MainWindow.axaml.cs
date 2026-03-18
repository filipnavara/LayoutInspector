using Avalonia.Controls;
using Avalonia.Input;
using Schaumamal.Models;
using Schaumamal.Models.Dumper;
using Schaumamal.ViewModels;

namespace Schaumamal.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not AppViewModel vm) return;
        if (e.KeyModifiers != KeyModifiers.Control) return;
        switch (e.Key)
        {
            case Key.D when vm.State != InspectorState.Waiting:
                _ = vm.ExtractAsync(new DumpProgressHandler((p, t) => { vm.DumpProgress = p; vm.DumpProgressText = t; }));
                e.Handled = true;
                break;
        }
    }
}
