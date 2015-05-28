using System;
using System.Threading;
using System.Windows;

namespace Narcissus
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static Mutex narMutex = new Mutex(false, "NARCISSUS_MUT");
        private Window mainWindow = null;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!narMutex.WaitOne(0, false))
            {
                narMutex.Close();
                narMutex = null;
                this.Shutdown();
            }
            else
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if  (narMutex != null)
            {
                narMutex.ReleaseMutex();
                narMutex.Close();
                narMutex = null;
            }
        }
    }
}
