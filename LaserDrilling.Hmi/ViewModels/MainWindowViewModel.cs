using Prism.Mvvm;

namespace LaserDrilling.Hmi.ViewModels
{
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
