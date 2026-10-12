using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Navigation;
using LaserDrilling.Hmi.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LaserDrilling.Hmi.ViewModels
{
    /// <summary>
    /// 协调主页导航、登录会话、子窗口和各功能视图模型。
    /// </summary>
    public sealed class WelcomeViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private string _selectedPage = "Workspace";
        public string SelectedPage { get => _selectedPage; private set { if (SetProperty(ref _selectedPage, value)) RaisePropertyChanged(nameof(IsConsoleEnabled)); } }
        private bool _settingsOpen;
        public bool IsConsoleEnabled => SelectedPage == "Workspace" && !_settingsOpen;
        internal void SetSettingsOpen(bool open)
        {
            _settingsOpen = open;
            if (open) { Operation?.Machine.EndJog(); Operation?.Machine.EndManualTest(); Operation?.CancelCommand.Execute(); }
            RaisePropertyChanged(nameof(IsConsoleEnabled));
        }
        public RecipeLibraryViewModel Recipes { get; private set; }
        private void SelectPage(string page) { Operation?.Machine.EndJog(); if (page != "Workspace") { Operation?.Machine.EndManualTest(); Operation?.CancelCommand.Execute(); } if (page == "Recipes" && Recipes?.ReloadCommand.CanExecute() == true) Recipes.ReloadCommand.Execute(); SelectedPage = page; }
        private string _userName = "";
        private readonly System.Windows.Threading.DispatcherTimer _clock = new() { Interval = System.TimeSpan.FromSeconds(1) };
        public string ClockText => System.DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        public string DateText => System.DateTime.Now.ToString("yyyy-MM-dd");
        public string TimeText => System.DateTime.Now.ToString("HH:mm:ss");
        private void ClockTick(object sender, System.EventArgs e) { RaisePropertyChanged(nameof(ClockText)); RaisePropertyChanged(nameof(DateText)); RaisePropertyChanged(nameof(TimeText)); }
        private readonly IAccountService _accounts;
        private readonly IRegionManager _regions;
        private readonly WorkspaceWindows _windows = new WorkspaceWindows();
        public DelegateCommand DeviceCommand { get; }
        public DelegateCommand RecipeCommand { get; }
        public DelegateCommand LogsCommand { get; }
        public DelegateCommand MaintenanceCommand { get; }
        public MaintenanceViewModel Maintenance { get; private set; }
        private AccessPanelViewModel _panel;
        private DrillingViewModel _operation;
        public DrillingViewModel Operation { get => _operation; private set => SetProperty(ref _operation, value); }
        public DelegateCommand OperationCommand { get; }
        public DelegateCommand SettingsCommand { get; }
        public DelegateCommand LanguageCommand { get; }
        public DelegateCommand ThemeCommand { get; }
        public AccessPanelViewModel Panel { get => _panel; private set => SetProperty(ref _panel, value); }
        public StartupOptions Brand { get; }
        public string UserName { get => _userName; private set => SetProperty(ref _userName, value); }
        public DelegateCommand SignOutCommand { get; }
        public bool KeepAlive => false;
        public WelcomeViewModel(IRegionManager regions, StartupOptions options, IAccountService accounts)
        {
            Brand = options;
            _accounts = accounts;
            _regions = regions;
            SignOutCommand = new DelegateCommand(() => { _windows.CloseAll(); accounts.SignOut(); regions.RequestNavigate("ContentRegion", nameof(LoginView)); }, () => Operation?.Machine.Busy != true && Operation?.Machine.IsJogging != true);
            OperationCommand = new DelegateCommand(() => { SelectPage("Workspace"); Operation?.Machine.RefreshRecipe(); });
            DeviceCommand = new DelegateCommand(() => SelectPage("Device"));
            MaintenanceCommand = new DelegateCommand(() => SelectPage("Maintenance"));
            LogsCommand = new DelegateCommand(() => SelectPage("Logs"));
            RecipeCommand = new DelegateCommand(() => SelectPage("Recipes"));
            SettingsCommand = new DelegateCommand(() => _windows.Open("Settings", this), () => Operation?.Machine.Busy != true && Operation?.Machine.IsJogging != true);
            LanguageCommand = new DelegateCommand(() => { App.Appearance.SetLanguage(!App.Appearance.IsEnglish); Panel?.RefreshLanguage(); Operation?.Machine.Changed(); });
            ThemeCommand = new DelegateCommand(() => App.Appearance.SetTheme(!App.Appearance.IsDark));
        }
        public void OnNavigatedTo(NavigationContext context)
        {
            if (_accounts.CurrentUser == null) { _regions.RequestNavigate("ContentRegion", nameof(LoginView)); return; }
            _clock.Tick += ClockTick; _clock.Start();
            UserName = _accounts.CurrentUser.UserName;
            Panel = new AccessPanelViewModel(_accounts);
            Operation = new DrillingViewModel(_accounts);
            Maintenance = new MaintenanceViewModel(_accounts, Operation.Machine);
            Recipes = new RecipeLibraryViewModel(_accounts, Operation.Machine);
            Operation.Machine.PropertyChanged += OnMachineChanged;
        }
        public bool IsNavigationTarget(NavigationContext context) => false;
        private void OnMachineChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) { SignOutCommand.RaiseCanExecuteChanged(); SettingsCommand.RaiseCanExecuteChanged(); }
        public void OnNavigatedFrom(NavigationContext context) { _clock.Stop(); _clock.Tick -= ClockTick; _windows.CloseAll(); Recipes?.Dispose(); Maintenance?.Dispose(); Panel?.Clear(); if (Operation != null) { Operation.Machine.PropertyChanged -= OnMachineChanged; Operation.Dispose(); } }
    }
}
