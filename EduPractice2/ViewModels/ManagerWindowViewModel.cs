using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduPractice2.Models;
using EduPractice2.Services;
using System;
using System.Windows.Input;

namespace EduPractice2.ViewModels
{
    public partial class ManagerWindowViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public new User CurrentUser { get; }
        public new Role CurrentRole { get; }
        public string WelcomeMessage { get; }
        public string UserRoleName => CurrentRole?.Name ?? "Неизвестно";

        public ICommand LogoutCommand { get; }

        public ManagerWindowViewModel(User user, Role role, DatabaseService dbService, MainWindowViewModel mainWindowViewModel)
            : base(user, role, dbService)
        {
            CurrentUser = user;
            CurrentRole = role;
            _dbService = dbService;
            _mainWindowViewModel = mainWindowViewModel;
            WelcomeMessage = $"Панель менеджера: {user.FullName ?? user.Login}";

            LogoutCommand = new RelayCommand(Logout);
        }

        private new void Logout()
        {
            App.CurrentUser = null;
            App.CurrentRole = null;
            _mainWindowViewModel.NavigateToLogin();
        }
    }
}