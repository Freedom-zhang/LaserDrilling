using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class WelcomeViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private string _userName = "";
        public StartupOptions Brand { get; }
        public string UserName { get => _userName; private set => SetProperty(ref _userName, value); }
        public DelegateCommand SignOutCommand { get; }
        public bool KeepAlive => false;
        public WelcomeViewModel(IRegionManager regions, StartupOptions options)
        {
            Brand = options;
            SignOutCommand = new DelegateCommand(() => regions.RequestNavigate("ContentRegion", nameof(LoginView)));
        }
        public void OnNavigatedTo(NavigationContext context) => UserName = context.Parameters.GetValue<string>("userName");
        public bool IsNavigationTarget(NavigationContext context) => false;
        public void OnNavigatedFrom(NavigationContext context) { }
    }
}
