using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class WelcomeViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        private string _selectedPage = "Workspace";
        public string SelectedPage { get => _selectedPage; private set => SetProperty(ref _selectedPage, value); }
        public RecipeLibraryViewModel Recipes { get; private set; }
        private void SelectPage(string page) { Operation?.Machine.EndJog(); if (page == "Recipes" && Recipes?.ReloadCommand.CanExecute() == true) Recipes.ReloadCommand.Execute(); SelectedPage = page; }
        private string _userName = "";
        private readonly System.Windows.Threading.DispatcherTimer _clock = new() { Interval = System.TimeSpan.FromSeconds(1) };
        public string ClockText => System.DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        private void ClockTick(object sender, System.EventArgs e) => RaisePropertyChanged(nameof(ClockText));
        private readonly IAccountService _accounts;
        private readonly IRegionManager _regions;
        private readonly WorkspaceWindows _windows = new WorkspaceWindows();
        public DelegateCommand DeviceCommand { get; }
        public DelegateCommand RecipeCommand { get; }
        public DelegateCommand LogsCommand { get; }
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
            Recipes = new RecipeLibraryViewModel(_accounts, Operation.Machine);
            Operation.Machine.PropertyChanged += OnMachineChanged;
        }
        public bool IsNavigationTarget(NavigationContext context) => false;
        private void OnMachineChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) { SignOutCommand.RaiseCanExecuteChanged(); SettingsCommand.RaiseCanExecuteChanged(); }
        public void OnNavigatedFrom(NavigationContext context) { _clock.Stop(); _clock.Tick -= ClockTick; _windows.CloseAll(); Recipes?.Dispose(); Panel?.Clear(); if (Operation != null) { Operation.Machine.PropertyChanged -= OnMachineChanged; Operation.Dispose(); } }
    }
}
