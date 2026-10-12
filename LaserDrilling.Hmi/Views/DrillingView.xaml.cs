using System.Windows;
using System.Windows.Controls;
namespace LaserDrilling.Hmi.Views
{
    public partial class DrillingView : UserControl
    {
        public DrillingView() => InitializeComponent();
        private void OnFit(object sender, RoutedEventArgs e) => Viewport.Fit();
    }
}
