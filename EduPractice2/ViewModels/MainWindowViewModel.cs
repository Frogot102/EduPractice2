using CommunityToolkit.Mvvm.ComponentModel;
using EduPractice2.Models;
using EduPractice2.Services;

namespace EduPractice2.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        public MainWindowViewModel(DatabaseService dbService) : base(null!, null!, dbService)
        {
            _dbService = dbService;
            _currentViewModel = new LoginViewModel(dbService, this);
        }

        public void NavigateToLogin()
        {
            CurrentViewModel = new LoginViewModel(_dbService, this);
        }

        public void NavigateByRole(User user, Role role)
        {
            CurrentViewModel = role.Name.ToLower() switch
            {
                "заказчик" => new CustomerWindowViewModel(user, role, _dbService, this),
                "менеджер" => new ManagerWindowViewModel(user, role, _dbService, this),
                "конструктор" => new DesignerWindowViewModel(user, role, _dbService, this),
                "мастер" => new MasterWindowViewModel(user, role, _dbService, this),
                "директор" => new DirectorWindowViewModel(user, role, _dbService, this),
                _ => new LoginViewModel(_dbService, this)
            };
        }
    }
}