using System.Windows.Controls;

namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// 承载系统主页、导航和公共工作区域，加载后恢复主窗口布局。
    /// </summary>
    public partial class WelcomeView : UserControl
    {
        public WelcomeView() => InitializeComponent();
        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.Windows.Window.GetWindow(this) is MainWindow window) window.SetLoginLayout(false);
        }
    }
}
