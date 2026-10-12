using System.Windows;
using System.Windows.Controls;
namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// 显示孔位编辑和加工工作区，处理视图适应等界面操作。
    /// </summary>
    public partial class DrillingView : UserControl
    {
        public DrillingView() => InitializeComponent();
        private void OnFit(object sender, RoutedEventArgs e) => Viewport.Fit();
    }
}
