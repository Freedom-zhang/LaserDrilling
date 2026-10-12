using System.Windows;
using System.Windows.Controls;
using LaserDrilling.Hmi.ViewModels;
namespace LaserDrilling.Hmi.Views
{
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
