using System.Threading;
using System.Windows;

namespace AutomaticUpdater;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private static bool _mutexOwned;
    private TrayApplication? _trayApp;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "AutomaticUpdater_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        _mutexOwned = createdNew;

        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "AutomaticUpdater is already running.",
                "AutomaticUpdater",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _trayApp = new TrayApplication();
        _trayApp.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayApp?.Dispose();
        if (_mutexOwned) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
