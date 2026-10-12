using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    // Owned modeless windows share the current authenticated machine session.
    public sealed class WorkspaceWindows
    {
        private readonly Dictionary<string, Window> _windows = new();
        public void Open(string key, WelcomeViewModel context, object contentContext = null)
        {
            if (_windows.TryGetValue(key, out var existing)) { (contentContext as IDisposable)?.Dispose(); existing.Activate(); return; }
            var view = (FrameworkElement)Application.LoadComponent(new Uri("/LaserDrilling.Hmi;component/Views/" + key + "Page.xaml", UriKind.Relative));
            view.DataContext = contentContext ?? (key == "Device" ? (object)context.Operation : context);
            var surface = new Border { Child = view };
            surface.SetResourceReference(Border.BackgroundProperty, "PageBrush");
            var window = new Window { Owner = Application.Current.MainWindow, Content = surface, Width = 920, Height = 720, MinWidth = 800, MinHeight = 660, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            if (key == "Recipes") { window.Width = System.Math.Min(1180, SystemParameters.WorkArea.Width); window.Height = System.Math.Min(920, SystemParameters.WorkArea.Height); window.MinWidth = 1000; }
            window.SetResourceReference(Window.BackgroundProperty, "PageBrush");
            window.SetResourceReference(Window.ForegroundProperty, "InkBrush");
            window.SetResourceReference(Window.TitleProperty, "Text.Machine.Nav" + key);
            window.Closed += (_, _) => { _windows.Remove(key); (contentContext as IDisposable)?.Dispose(); if (key == "Settings") context.Panel?.Clear(); };
            _windows.Add(key, window);
            window.Show();
        }
        public void CloseAll() { foreach (var window in new List<Window>(_windows.Values)) window.Close(); }
    }
}
