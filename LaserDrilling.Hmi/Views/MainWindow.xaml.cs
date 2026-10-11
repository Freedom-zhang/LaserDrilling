using System.Windows;

namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public void SetLoginLayout(bool login)
        {
            WindowState = WindowState.Normal;
            WindowStyle = login ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            ResizeMode = login ? ResizeMode.NoResize : ResizeMode.CanResize;
            MinWidth = login ? 760 : 940;
            MinHeight = login ? 540 : 640;
            Width = login ? 760 : 1100;
            Height = login ? 540 : 720;
            var area = SystemParameters.WorkArea;
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
        }
    }
}
