using System.Configuration;
using System.Data;
using System.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace CustomClipboardManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            appPath = appPath.Replace(".dll", ".exe");
            rk.SetValue("CustomClipboardManager", appPath);
        }
        catch { }
    }
}
