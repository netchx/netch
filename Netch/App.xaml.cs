using System.Windows;

namespace Netch
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.MainWindow = new Forms.MainWindow();
            this.MainWindow.Show();
        }
    }
}
