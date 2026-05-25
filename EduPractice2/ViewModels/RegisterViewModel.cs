using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduPractice2.Services;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EduPractice2.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public RegisterViewModel(DatabaseService dbService, MainWindowViewModel mainWindowViewModel)
            : base(null!, null!, dbService)
        {
            _mainWindowViewModel = mainWindowViewModel;

            RegisterCommand = new RelayCommand(RegisterAsync);
            BackToLoginCommand = new RelayCommand(BackToLogin);
        }

        private bool ValidatePassword(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (password.Length < 4 || password.Length > 16)
            {
                errorMessage = "Пароль должен содержать от 4 до 16 символов";
                return false;
            }

            string forbiddenPattern = @"[*&{}|+]";
            if (Regex.IsMatch(password, forbiddenPattern))
            {
                errorMessage = "Пароль не должен содержать символы: * & { } | +";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-ZА-ЯЁ]"))
            {
                errorMessage = "Пароль должен содержать хотя бы одну заглавную букву";
                return false;
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage = "Пароль должен содержать хотя бы одну цифру";
                return false;
            }

            return true;
        }

        private async void RegisterAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Заполните логин и пароль";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return;
            }

            if (!ValidatePassword(Password, out string validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            try
            {
                int customerId = 1;

                var success = await DbService.RegisterUserAsync(
                    Login, Password, customerId,
                    string.IsNullOrWhiteSpace(FullName) ? null : FullName);

                if (success)
                {
                    _mainWindowViewModel.NavigateToLogin();
                }
                else
                {
                    ErrorMessage = "Пользователь с таким логином уже существует";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Ошибка регистрации: {ex.Message}";
            }
        }

        private void BackToLogin()
        {
            _mainWindowViewModel.NavigateToLogin();
        }
    }
}