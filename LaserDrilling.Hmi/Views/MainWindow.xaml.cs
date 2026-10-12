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
            var area = SystemParameters.WorkArea;
            MinWidth = login ? 760 : 1180;
            MinHeight = login ? 540 : 740;
            Width = login ? 760 : System.Math.Min(1440, area.Width);
            Height = login ? 540 : System.Math.Min(920, area.Height);
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
        }
    }
}
