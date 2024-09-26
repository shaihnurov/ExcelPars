using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Threading;

namespace ExcelPars.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                SetProperty(ref _currentView, value);
            }
        }

        public RelayCommand ExcelViewCommand { get; set; }
        public RelayCommand Tt2ViewCommand { get; set; }
        public RelayCommand Tt3ViewCommand { get; set; }

        private readonly ExcelViewModel _exelViewModel;
        private readonly TechnicalTask2ViewModel _tt2ViewModel;
        private readonly TechnicalTask3ViewModel _tt3ViewModel;

        public MainViewModel()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-EN");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-EN");

            _exelViewModel = new ExcelViewModel();
            _tt2ViewModel = new TechnicalTask2ViewModel();
            _tt3ViewModel = new TechnicalTask3ViewModel();
            _currentView = _exelViewModel;

            ExcelViewCommand = new RelayCommand(() =>
            {
                CurrentView = _exelViewModel;
            });
            Tt2ViewCommand = new RelayCommand(() =>
            {
                CurrentView = _tt2ViewModel;
            });
            Tt3ViewCommand = new RelayCommand(() =>
            {
                CurrentView = _tt3ViewModel;
            });
        }
    }
}
