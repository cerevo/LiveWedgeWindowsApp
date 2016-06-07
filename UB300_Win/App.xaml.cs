using System.Windows;
using Cerevo.UB300_Win.Media;

namespace Cerevo.UB300_Win {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e) {
            MediaUtils.MediaFoundationStartup();
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            try {
                MediaUtils.MediaFoundationShutdown();
            } catch { }
        }
    }
}
