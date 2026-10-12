using System.Windows;
using System.Windows.Controls;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// 显示登录页面，处理密码输入及登录窗口的界面交互。
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView() => InitializeComponent();
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow window) window.SetLoginLayout(true);
            PasswordInput.Focus();
        }
        private void OnLanguage(object sender, RoutedEventArgs e) => App.Appearance.SetLanguage(!App.Appearance.IsEnglish);
        private void OnTheme(object sender, RoutedEventArgs e) => App.Appearance.SetTheme(!App.Appearance.IsDark);
        private void OnClose(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();
        private void OnMinimize(object sender, RoutedEventArgs e) { if (Window.GetWindow(this) is Window window) window.WindowState = WindowState.Minimized; }
        private void OnDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) Window.GetWindow(this)?.DragMove();
        }
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm) vm.UpdatePassword(PasswordInput.Password);
        }
        private void OnConfirmationChanged(object sender, RoutedEventArgs e)
        { if (DataContext is LoginViewModel vm) vm.UpdateConfirmation(ConfirmationInput.Password); }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm) vm.ClearSensitiveData();
            PasswordInput.Clear();
            ConfirmationInput.Clear();
        }
    }
}
