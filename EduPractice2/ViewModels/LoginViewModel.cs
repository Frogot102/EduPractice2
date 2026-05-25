using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduPractice2.Models;
using EduPractice2.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EduPractice2.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly string _configPath = Path.Combine(Environment.CurrentDirectory, "user_credentials.json");

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string? _errorMessage;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public ICommand LoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        public LoginViewModel(DatabaseService dbService, MainWindowViewModel mainWindowViewModel)
            : base(null!, null!, dbService)
        {
            _mainWindowViewModel = mainWindowViewModel;

            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            OpenRegisterCommand = new RelayCommand(OpenRegister);

            LoadSavedCredentials();

            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Login) || e.PropertyName == nameof(Password))
                {
                    ((RelayCommand)LoginCommand).NotifyCanExecuteChanged();
                }
            };
        }

        private void LoadSavedCredentials()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var credentials = JsonSerializer.Deserialize<UserCredentials>(json);
                    if (credentials != null)
                    {
                        Login = credentials.Login;
                        Password = credentials.Password;
                        RememberMe = true;
                    }
                }
            }
            catch { }
        }

        private void SaveCredentials()
        {
            try
            {
                if (RememberMe)
                {
                    var credentials = new UserCredentials { Login = Login, Password = Password };
                    File.WriteAllText(_configPath, JsonSerializer.Serialize(credentials));
                }
                else
                {
                    if (File.Exists(_configPath)) File.Delete(_configPath);
                }
            }
            catch { }
        }

        private bool CanExecuteLogin() => !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);

        private async void ExecuteLogin()
        {
            ErrorMessage = string.Empty;

            try
            {
                User? user = await DbService.AuthenticateAsync(Login, Password);

                if (user != null)
                {
                    Role? role = await DbService.GetUserRoleAsync(user.RoleId);
                    App.CurrentUser = user;
                    App.CurrentRole = role;
                    SaveCredentials();

                    _mainWindowViewModel.NavigateByRole(user, role!);
                }
                else
                {
                    ErrorMessage = "Неверный логин или пароль!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка БД: {ex.Message}";
            }
        }

        private void OpenRegister()
        {
            _mainWindowViewModel.CurrentViewModel = new RegisterViewModel(DbService, _mainWindowViewModel);
        }
    }

    public class UserCredentials
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}