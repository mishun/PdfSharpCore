using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MigraDocCore.Avalonia.Samples
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var model = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow() { DataContext = model };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
