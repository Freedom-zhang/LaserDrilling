using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    public partial class ControlConsole : UserControl
    {
        private Window _owner;
        private Button _held;
        public ControlConsole()
        {
            InitializeComponent();
            Loaded += (_, _) => { _owner = Window.GetWindow(this); if (_owner != null) _owner.Deactivated += Deactivated; };
            Unloaded += (_, _) => { Release(); if (_owner != null) _owner.Deactivated -= Deactivated; _owner = null; };
        }
        private void Deactivated(object sender, EventArgs e) { Release(); if (DataContext is DrillingViewModel vm) vm.Machine.EndManualTest(); }
        private void ZDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && DataContext is DrillingViewModel vm && vm.Machine.CanZJog)
            {
                _held = button; button.CaptureMouse(); vm.Machine.BeginZJog(button.Tag?.ToString() == "+" ? 1 : -1); e.Handled = true;
            }
        }
        private void ZUp(object sender, MouseButtonEventArgs e) { Release(); e.Handled = true; }
        private void ZLeave(object sender, MouseEventArgs e) => Release();
        private void ZDisabled(object sender, DependencyPropertyChangedEventArgs e) { if (!(bool)e.NewValue) Release(); }
        private void ZKey(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Release(); }
        private void Release()
        {
            var held = _held; _held = null;
            if (held == null) return;
            if (DataContext is DrillingViewModel vm) vm.Machine.EndJog();
            if (held.IsMouseCaptured) held.ReleaseMouseCapture();
        }
    }
}
