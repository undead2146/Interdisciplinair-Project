using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.ViewModels
{
    public class CreateShowViewModel : INotifyPropertyChanged
    {
        private Window _window;
        private string _showName;

        public string ShowName
        {
            get => _showName;
            set
            {
                if (_showName != value)
                {
                    _showName = value;
                    OnPropertyChanged();
                    // ❌ CommandManager.InvalidateRequerySuggested() doesn’t affect RelayCommand
                    // ✅ Instead, manually raise CanExecuteChanged:
                    (OKCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public ICommand OKCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public CreateShowViewModel()
        {
            OKCommand = new RelayCommand(ExecuteOK, CanExecuteOK);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public void SetWindow(Window window)
        {
            _window = window;
        }

        private bool CanExecuteOK() =>
            !string.IsNullOrWhiteSpace(ShowName);

        private void ExecuteOK()
        {
            _window.DialogResult = true;
            _window.Close();
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null
        )
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}