using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using LaserDrilling.Hmi.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.ViewModels
{
    /// <summary>
    /// 管理用户创建及受权限限制的参数配置，并提供校验和操作反馈。
    /// </summary>
    public sealed class AccessPanelViewModel : BindableBase
    {
        private readonly IAccountService _accounts;
        private bool _busy;
        private string _message = "";
        private string _newName = "";
        private string _password = "";
        private bool _newAdministrator;
        public bool CanAdmin => _accounts.CurrentUser?.Role == UserRole.Administrator;
        public bool CanEdit => CanAdmin && !_busy;
        public string RoleName => App.Appearance.Text(CanAdmin ? "Administrator" : "Operator");
        public string Message => App.Appearance.Text(_message);
        public bool HasMessage => !string.IsNullOrEmpty(_message);
        public bool HasError => HasMessage && _message != "UserCreated" && _message != "ParametersSaved";
        public string NewName { get => _newName; set => SetProperty(ref _newName, value); }
        public bool NewAdministrator { get => _newAdministrator; set => SetProperty(ref _newAdministrator, value); }
        public ObservableCollection<UserIdentity> Users { get; } = new ObservableCollection<UserIdentity>();
        private string _power, _frequency, _pulses;
        public string Power { get => _power; set => SetProperty(ref _power, value); }
        public string Frequency { get => _frequency; set => SetProperty(ref _frequency, value); }
        public string Pulses { get => _pulses; set => SetProperty(ref _pulses, value); }
        public DelegateCommand CreateUserCommand { get; }
        public DelegateCommand SaveParametersCommand { get; }
        public AccessPanelViewModel(IAccountService accounts)
        {
            _accounts = accounts;
            var parameters = accounts.GetParameters();
            Power = parameters.PowerPercent.ToString(CultureInfo.InvariantCulture);
            Frequency = parameters.FrequencyKhz.ToString(CultureInfo.InvariantCulture);
            Pulses = parameters.PulseCount.ToString(CultureInfo.InvariantCulture);
            CreateUserCommand = new DelegateCommand(async () => await CreateUserAsync(), () => CanEdit);
            SaveParametersCommand = new DelegateCommand(SaveParameters, () => CanEdit);
            RefreshUsers();
        }
        public void UpdatePassword(string password) => _password = password;
        private void Show(string key) { _message = key; RaisePropertyChanged(nameof(Message)); RaisePropertyChanged(nameof(HasMessage)); RaisePropertyChanged(nameof(HasError)); }
        private void RefreshUsers() { if (!CanAdmin) return; Users.Clear(); foreach (var user in _accounts.GetUsers()) Users.Add(user); }
        public async Task<bool> CreateUserAsync()
        {
            if (_busy) return false;
            if (!CanAdmin) { Show("AccessDenied"); return false; }
            var validationError = AccountValidation.Validate(NewName, _password);
            if (validationError != null) { Show(validationError); return false; }
            _busy = true; RefreshCommands();
            try
            {
                await _accounts.CreateUserAsync(NewName, _password, NewAdministrator ? UserRole.Administrator : UserRole.Operator);
                _password = ""; NewName = ""; NewAdministrator = false; RefreshUsers(); Show("UserCreated");
                return true;
            }
            catch (ArgumentException ex) { Show(ex.Message == "InvalidUserName" || ex.Message == "InvalidPassword" ? ex.Message : "StoreError"); }
            catch (InvalidOperationException ex) { Show(ex.Message == "DuplicateUser" ? "DuplicateUser" : "StoreError"); }
            catch (UnauthorizedAccessException) { Show("AccessDenied"); }
            catch (Exception) { Show("StoreError"); }
            finally { _busy = false; RefreshCommands(); }
            return false;
        }
        private void RefreshCommands() { RaisePropertyChanged(nameof(CanEdit)); CreateUserCommand.RaiseCanExecuteChanged(); SaveParametersCommand.RaiseCanExecuteChanged(); }
        public void SaveParameters()
        {
            try
            {
                if (!double.TryParse(Power, NumberStyles.Float, CultureInfo.InvariantCulture, out var power) || !double.TryParse(Frequency, NumberStyles.Float, CultureInfo.InvariantCulture, out var frequency) || !int.TryParse(Pulses, out var pulses))
                    throw new ArgumentException("InvalidParameters");
                _accounts.SaveParameters(new ProcessParameters(power, frequency, pulses)); Show("ParametersSaved");
            }
            catch (ArgumentException) { Show("InvalidParameters"); }
            catch (UnauthorizedAccessException) { Show("AccessDenied"); }
            catch (Exception) { Show("StoreError"); }
        }
        public void Clear() => _password = "";
        public void RefreshLanguage() { RaisePropertyChanged(nameof(RoleName)); RaisePropertyChanged(nameof(Message)); }
    }
}
