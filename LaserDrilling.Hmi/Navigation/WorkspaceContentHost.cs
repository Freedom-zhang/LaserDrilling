using LaserDrilling.Hmi.Views;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Navigation
{
    /// <summary>
    /// 切换并缓存当前登录会话的工作页面，为各页面分配共享视图模型。
    /// </summary>
    public sealed class WorkspaceContentHost : ContentControl
    {
        private readonly Dictionary<string, FrameworkElement> _pages = new();
        public static readonly DependencyProperty SelectedPageProperty = DependencyProperty.Register(nameof(SelectedPage), typeof(string), typeof(WorkspaceContentHost), new PropertyMetadata("Workspace", (d, e) => ((WorkspaceContentHost)d).ShowPage()));
        public string SelectedPage { get => (string)GetValue(SelectedPageProperty); set => SetValue(SelectedPageProperty, value); }
        public WorkspaceContentHost()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            DataContextChanged += (_, _) => { _pages.Clear(); ShowPage(); };
            Loaded += (_, _) => ShowPage();
        }
        private void ShowPage()
        {
            if (DataContext is not WelcomeViewModel vm || vm.Operation == null || vm.Recipes == null) return;
            var key = SelectedPage ?? "Workspace";
            if (!_pages.TryGetValue(key, out var view))
            {
                view = key == "Workspace" ? new DrillingView() : (FrameworkElement)Application.LoadComponent(new System.Uri($"/LaserDrilling.Hmi;component/Views/{key}Page.xaml", System.UriKind.Relative));
                view.DataContext = key == "Maintenance" ? vm.Maintenance : key == "Recipes" ? vm.Recipes : key == "Logs" ? vm : vm.Operation;
                _pages.Add(key, view);
            }
            Content = view;
        }
    }
}
