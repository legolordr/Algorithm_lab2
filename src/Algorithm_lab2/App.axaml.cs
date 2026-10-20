using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Algorithm_lab2.Theme;
using Algorithm_lab2.Views;

namespace Algorithm_lab2;

public partial class App : Application
{
    public override void Initialize()
    {
        Palette.Register(Resources);
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
