using Avalonia.Input;

namespace Algorithm_lab2.Views;

/// <summary>
/// A page that reacts to the shared keyboard shortcuts: Space — play / pause, Right — step,
/// R — start over, N — new input data. Pages map only the keys that make sense for them.
/// </summary>
public interface IShortcutHandler
{
    /// <returns>True if the page used the key.</returns>
    bool HandleShortcut(Key key);
}
