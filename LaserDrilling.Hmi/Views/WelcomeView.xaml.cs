using System.Windows.Controls;

namespace LaserDrilling.Hmi.Views
{
    public partial class WelcomeView : UserControl
    {
        public WelcomeView() => InitializeComponent();
        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.Windows.Window.GetWindow(this) is MainWindow window) window.SetLoginLayout(false);
        }
    }
}
