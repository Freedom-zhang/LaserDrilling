using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    public partial class SplashView : UserControl
    {
        public SplashView() => InitializeComponent();
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Start the display interval after the initial layout/render work has run.
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            if (IsLoaded && DataContext is SplashViewModel vm) await vm.StartAsync();
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SplashViewModel vm) vm.Cancel();
        }
    }
}
