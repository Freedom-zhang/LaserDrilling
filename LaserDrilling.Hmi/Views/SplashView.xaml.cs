using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// 显示公司启动信息，在首次界面布局完成后启动展示计时。
    /// </summary>
    public partial class SplashView : UserControl
    {
        public SplashView() => InitializeComponent();
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 首次布局和渲染完成后，再开始计算启动页展示时间。
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            if (IsLoaded && DataContext is SplashViewModel vm) await vm.StartAsync();
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SplashViewModel vm) vm.Cancel();
        }
    }
}
