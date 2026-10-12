using System.Windows;
using System.Windows.Controls;
using LaserDrilling.Hmi.ViewModels;
namespace LaserDrilling.Hmi.Views
{
    /// <summary>
    /// 显示用户管理和权限相关配置，将密码输入等界面事件转交视图模型。
    /// </summary>
    public partial class AccessPanel : UserControl
    {
        public AccessPanel() => InitializeComponent();
        private void OnPasswordChanged(object sender, RoutedEventArgs e) { if (DataContext is AccessPanelViewModel vm) vm.UpdatePassword(NewPasswordInput.Password); }
        private async void OnCreateUser(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccessPanelViewModel vm && await vm.CreateUserAsync()) NewPasswordInput.Clear();
        }
        private void OnUnloaded(object sender, RoutedEventArgs e) { NewPasswordInput.Clear(); if (DataContext is AccessPanelViewModel vm) vm.Clear(); }
    }
}
