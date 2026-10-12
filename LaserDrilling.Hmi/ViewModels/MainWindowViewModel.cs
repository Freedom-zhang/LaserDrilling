using Prism.Mvvm;

namespace LaserDrilling.Hmi.ViewModels
{
    /// <summary>
    /// 提供主窗口标题等供窗口绑定的基础属性。
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(LaserDrilling.Hmi.Services.StartupOptions options)
        {
            Title = options.CompanyName + " · " + options.ProductName;
        }
    }
}
