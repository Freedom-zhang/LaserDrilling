using System;
using System.Threading;
using System.Threading.Tasks;
using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Views;
using Prism.Mvvm;
using Prism.Regions;

namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class SplashViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private readonly IRegionManager _regions;
        private CancellationTokenSource _lifetime;
        private bool _started;
        private string _status = "Loading";
        public StartupOptions Brand { get; }
        public string Status { get => App.Appearance.Text(_status); private set => SetProperty(ref _status, value); }
        public bool KeepAlive => false;

        public SplashViewModel(IRegionManager regions, StartupOptions options)
        {
            _regions = regions;
            Brand = options;
        }

        public async Task StartAsync()
        {
            if (_started) return;
            _started = true;
            var lifetime = new CancellationTokenSource();
            _lifetime = lifetime;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Brand.SplashDurationSeconds), lifetime.Token);
                if (lifetime.IsCancellationRequested) return;
                _regions.RequestNavigate("ContentRegion", nameof(LoginView), result =>
                {
                    if (result.Result != true) Status = "SplashError";
                });
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(_lifetime, lifetime)) _lifetime = null;
                lifetime.Dispose();
            }
        }
        public void Cancel() => _lifetime?.Cancel();
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) => Cancel();
    }
}
