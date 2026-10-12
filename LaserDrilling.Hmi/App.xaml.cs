using System.Windows;
using LaserDrilling.Hmi.Views;
using Prism.Ioc;
using Prism.Regions;
using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi
{
    /// <summary>
    /// 应用程序入口，配置 Prism 容器、页面导航及全局服务。
    /// </summary>
    public partial class App
    {
        public static AppearanceService Appearance { get; } = new AppearanceService();

        protected override Window CreateShell()
        {
            Appearance.SetProductName(Container.Resolve<StartupOptions>().ProductName);
            Appearance.SetLanguage(false);
            Appearance.SetTheme(false);
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(StartupOptions.Load());
            containerRegistry.RegisterInstance<IAccountService>(new LocalAccountService());
            containerRegistry.RegisterForNavigation<SplashView, SplashViewModel>();
            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
            containerRegistry.RegisterForNavigation<WelcomeView, WelcomeViewModel>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Container.Resolve<IRegionManager>().RequestNavigate("ContentRegion", nameof(SplashView));
        }
    }
}
