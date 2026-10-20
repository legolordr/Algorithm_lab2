using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Algorithm_lab2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Tunnel so the shortcut wins before a focused button turns Space into its own click.
        AddHandler(KeyDownEvent, OnShortcutKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        // Keys typed into a text field (e.g. NumericUpDown) belong to that field.
        if (e.KeyModifiers != KeyModifiers.None || FocusManager?.GetFocusedElement() is TextBox)
            return;

        if (Pages.SelectedItem is IShortcutHandler page && page.HandleShortcut(e.Key))
            e.Handled = true;
    }
}
