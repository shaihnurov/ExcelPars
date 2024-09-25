using CommunityToolkit.Mvvm.ComponentModel;

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

        private readonly ExcelViewModel _exelViewModel;

        public MainViewModel()
        {
            _exelViewModel = new ExcelViewModel();
            _currentView = _exelViewModel;
        }
    }
}
