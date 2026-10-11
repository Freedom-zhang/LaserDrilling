using System;
using System.Threading;
using System.Threading.Tasks;
using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class LoginViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private readonly IDemoAuthenticationService _authentication;
        private readonly IRegionManager _regions;
        private CancellationTokenSource _pendingLogin;
        private string _userName = "operator";
        private string _password = "";
        private string _errorMessage = "";
        private bool _isBusy;
        public StartupOptions Brand { get; }
        public string UserName { get => _userName; set { SetProperty(ref _userName, value); ErrorMessage = ""; } }
        public string ErrorMessage { get => App.Appearance.Text(_errorMessage); private set { if (SetProperty(ref _errorMessage, value)) RaisePropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { RaisePropertyChanged(nameof(IsIdle)); RaisePropertyChanged(nameof(LoginCaption)); SignInCommand.RaiseCanExecuteChanged(); } } }
        public bool IsIdle => !IsBusy;
        public string LoginCaption => App.Appearance.Text(IsBusy ? "SigningIn" : "SignIn");
        public bool KeepAlive => false;
        public DelegateCommand SignInCommand { get; }

        public LoginViewModel(IRegionManager regions, StartupOptions options, IDemoAuthenticationService authentication)
        {
            _regions = regions;
            System.ComponentModel.PropertyChangedEventManager.AddHandler(App.Appearance, OnAppearanceChanged, "");
            Brand = options;
            _authentication = authentication;
            SignInCommand = new DelegateCommand(async () => await SignInAsync(), () => !IsBusy);
        }
        private void OnAppearanceChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        { RaisePropertyChanged(nameof(ErrorMessage)); RaisePropertyChanged(nameof(LoginCaption)); }
        // PasswordBox event bridge; never logged or persisted.
        public void UpdatePassword(string password) { _password = password ?? ""; ErrorMessage = ""; }
        public async Task SignInAsync()
        {
            if (IsBusy) return;
            if (string.IsNullOrWhiteSpace(UserName)) { ErrorMessage = "AccountRequired"; return; }
            if (string.IsNullOrEmpty(_password)) { ErrorMessage = "PasswordRequired"; return; }
            ErrorMessage = "";
            IsBusy = true;
            var request = new CancellationTokenSource();
            _pendingLogin = request;
            try
            {
                if (!await _authentication.SignInAsync(UserName.Trim(), _password, request.Token))
                {
                    ErrorMessage = "InvalidCredentials";
                    return;
                }
                if (request.IsCancellationRequested) return;
                var parameters = new NavigationParameters { { "userName", UserName.Trim() } };
                _regions.RequestNavigate("ContentRegion", nameof(WelcomeView), result =>
                {
                    if (result.Result != true) ErrorMessage = "NavigationError";
                }, parameters);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { ErrorMessage = "ServiceError"; }
            finally
            {
                if (ReferenceEquals(_pendingLogin, request)) _pendingLogin = null;
                request.Dispose();
                IsBusy = false;
            }
        }
        public void ClearSensitiveData() { _pendingLogin?.Cancel(); _password = ""; }
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) => ClearSensitiveData();
    }
}
