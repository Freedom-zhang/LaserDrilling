using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LaserDrilling.Hmi;
using LaserDrilling.Hmi.Views;
using LaserDrilling.Hmi.ViewModels;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        app.Startup += (_, _) => app.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                var window = app.MainWindow;
                var timer = Stopwatch.StartNew();
                await Task.Delay(100);
                Require(Find<SplashView>(window) != null, "Startup view visible");
                Capture(window, "startup-preview.png");
                while (Find<LoginView>(window) == null && timer.Elapsed.TotalSeconds < 5) await Task.Delay(20);
                var login = Find<LoginView>(window);
                Require(login != null, "Navigated to login");
                Require(timer.Elapsed.TotalSeconds >= 1.8 && timer.Elapsed.TotalSeconds < 4, "Default splash duration near 2 seconds: " + timer.Elapsed.TotalSeconds.ToString("F2"));
                await Task.Delay(100);
                Capture(window, "login-preview.png");
                Require(window.WindowStyle == WindowStyle.None && window.Width == 760 && window.Height == 540, "Compact borderless login window");
                var vm = (LoginViewModel)login.DataContext;
                await vm.SignInAsync();
                Require(vm.HasError, "Empty password rejected");
                Click(login, "中文 / EN");
                Require(vm.ErrorMessage == "Enter your password." && vm.LoginCaption == "Sign in", "Existing validation and command captions translate immediately");
                Click(login, "Dark");
                await Task.Delay(100);
                Require(((SolidColorBrush)app.FindResource("SurfaceBrush")).Color == (Color)ColorConverter.ConvertFromString("#203342"), "Dark palette applied");
                Capture(window, "login-dark-english.png");
                App.Appearance.SetTheme(false);
                await Task.Delay(50);
                Capture(window, "login-light-english.png");
                App.Appearance.SetLanguage(false);
                App.Appearance.SetTheme(true);
                await Task.Delay(50);
                Capture(window, "login-dark-chinese.png");
                App.Appearance.SetTheme(false);
                vm.UpdatePassword("wrong");
                await vm.SignInAsync();
                Require(vm.HasError && Find<LoginView>(window) != null, "Wrong password rejected");
                vm.UpdatePassword("Demo@123");
                await vm.SignInAsync();
                await Task.Delay(100);
                var welcome = Find<WelcomeView>(window);
                Require(welcome != null, "Correct demo login enters welcome");
                Require(window.WindowStyle == WindowStyle.SingleBorderWindow && window.Width == 1100, "Main system restores normal window");
                var welcomeVm = (WelcomeViewModel)welcome.DataContext;
                Require(welcomeVm.UserName == "operator", "Username passed through Prism navigation");
                welcomeVm.SignOutCommand.Execute();
                await Task.Delay(100);
                login = Find<LoginView>(window);
                Require(login != null, "Logout returns to login");
                Require(window.WindowStyle == WindowStyle.None && window.Width == 760, "Logout restores compact borderless window");
                await ((LoginViewModel)login.DataContext).SignInAsync();
                Require(((LoginViewModel)login.DataContext).HasError, "Logout clears password");
                await Task.Delay(100);
                Capture(window, "login-minimum-size.png");
                var drawing = new DrawingVisual();
                using (var dc = drawing.RenderOpen()) dc.DrawImage((ImageSource)app.FindResource("CompanyLogo"), new Rect(0, 0, 256, 256));
                Save(drawing, 256, 256, "LaserDrilling.Hmi/Assets/Brand/company-logo.png");
                Console.WriteLine("PASS: all startup/login smoke checks");
                app.Shutdown(0);
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); app.Shutdown(1); }
        }));
        Environment.ExitCode = app.Run();
    }
    static void Click(DependencyObject parent, string content)
    {
        var button = FindButton(parent, content) ?? throw new Exception("Button not found: " + content);
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    }
    static System.Windows.Controls.Button FindButton(DependencyObject parent, string content)
    {
        if (parent is System.Windows.Controls.Button button && button.Content?.ToString() == content) return button;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var found = FindButton(VisualTreeHelper.GetChild(parent, i), content); if (found != null) return found; }
        return null;
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    static T Find<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match) return match;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var found = Find<T>(VisualTreeHelper.GetChild(parent, i)); if (found != null) return found; }
        return null;
    }
    static void Capture(Window window, string name)
    {
        var content = (FrameworkElement)window.Content;
        content.UpdateLayout();
        Directory.CreateDirectory("work/previews");
        Save(content, (int)content.ActualWidth, (int)content.ActualHeight, "work/previews/" + name);
    }
    static void Save(Visual visual, int width, int height, string path)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
