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
    /// <summary>
    /// 管理账户初始化和登录流程、输入校验及登录成功后的页面导航。
    /// </summary>
    public sealed class LoginViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private readonly IAccountService _authentication;
        private readonly IRegionManager _regions;
        private CancellationTokenSource _pendingLogin;
        private string _userName = "";
        private string _password = "";
        private string _errorMessage = "";
        private bool _isBusy;
        public StartupOptions Brand { get; }
        public bool IsSetup { get; private set; }
        private string _confirmation = "";
        public void UpdateConfirmation(string value) => _confirmation = value;
        public string PageTitle => App.Appearance.Text(IsSetup ? "SetupTitle" : "LoginTitle");
        public string PageHint => App.Appearance.Text(IsSetup ? "SetupHint" : "LoginSubtitle");
        public string UserName { get => _userName; set { SetProperty(ref _userName, value); ErrorMessage = ""; } }
        public string ErrorMessage { get => App.Appearance.Text(_errorMessage); private set { if (SetProperty(ref _errorMessage, value)) RaisePropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { RaisePropertyChanged(nameof(IsIdle)); RaisePropertyChanged(nameof(LoginCaption)); SignInCommand.RaiseCanExecuteChanged(); } } }
        public bool IsIdle => !IsBusy;
        public string LoginCaption => App.Appearance.Text(IsBusy ? "SigningIn" : (IsSetup ? "SetupAction" : "SignIn"));
        public bool KeepAlive => false;
        public DelegateCommand SignInCommand { get; }

        public LoginViewModel(IRegionManager regions, StartupOptions options, IAccountService authentication)
        {
            _regions = regions;
            System.ComponentModel.PropertyChangedEventManager.AddHandler(App.Appearance, OnAppearanceChanged, "");
            Brand = options;
            _authentication = authentication;
            IsSetup = authentication.NeedsSetup;
            _userName = IsSetup ? "admin" : authentication.LastSignedInUserName;
            SignInCommand = new DelegateCommand(async () => await SignInAsync(), () => !IsBusy);
        }
        private void OnAppearanceChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        { RaisePropertyChanged(nameof(ErrorMessage)); RaisePropertyChanged(nameof(LoginCaption)); RaisePropertyChanged(nameof(PageTitle)); RaisePropertyChanged(nameof(PageHint)); }
        // 接收密码框事件传递的密码，不写入日志或持久化存储。
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
                if (IsSetup)
                {
                    if (_password != _confirmation) { ErrorMessage = "PasswordMismatch"; return; }
                    await _authentication.InitializeAdministratorAsync(UserName.Trim(), _password, request.Token);
                    IsSetup = false;
                    RaisePropertyChanged(nameof(IsSetup)); RaisePropertyChanged(nameof(PageTitle)); RaisePropertyChanged(nameof(PageHint));
                }
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
            catch (ArgumentException ex) { ErrorMessage = ex.Message; }
            catch (InvalidOperationException) { ErrorMessage = "SetupCompleted"; }
            catch (Exception) { ErrorMessage = "ServiceError"; }
            finally
            {
                if (ReferenceEquals(_pendingLogin, request)) _pendingLogin = null;
                request.Dispose();
                IsBusy = false;
            }
        }
        public void ClearSensitiveData() { _pendingLogin?.Cancel(); _password = ""; _confirmation = ""; }
        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) => ClearSensitiveData();
    }
}
