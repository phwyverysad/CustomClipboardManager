using System.Windows;
using Microsoft.Win32;

namespace CustomClipboardManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        try
        {
            Program.LogException("App.OnStartup", new Exception("Application started with ShutdownMode=OnExplicitShutdown"));
            using RegistryKey? rk = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            string appPath = Environment.ProcessPath ?? string.Empty;
            if (rk != null && !string.IsNullOrEmpty(appPath))
            {
                rk.SetValue("CustomClipboardManager", $"\"{appPath}\" --background");
            }
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Program.LogException("App.OnExit", new Exception($"Application exiting with code {e.ApplicationExitCode}"));
            CustomClipboardManager.Services.ServiceManager.StopServiceSync();
        }
        catch { }
        base.OnExit(e);
    }
}
