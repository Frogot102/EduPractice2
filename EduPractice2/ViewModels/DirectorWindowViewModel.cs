using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduPractice2.Models;
using EduPractice2.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EduPractice2.ViewModels
{
    public partial class DirectorWindowViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        // Данные формы
        [ObservableProperty]
        private string _formLogin = string.Empty;

        [ObservableProperty]
        private string _formPassword = string.Empty;

        [ObservableProperty]
        private Role? _selectedRole;

        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        [ObservableProperty]
        private string _formFullName = string.Empty;

        [ObservableProperty]
        private DateTimeOffset _formBirthDate = DateTimeOffset.Now.AddYears(-20);

        [ObservableProperty]
        private string _formAddress = string.Empty;

        [ObservableProperty]
        private string _formEducation = string.Empty;

        [ObservableProperty]
        private string _formQualification = string.Empty;

        [ObservableProperty]
        private string _formOperations = string.Empty;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private int _editingId = 0;

        [ObservableProperty]
        private string _formTitle = "Добавление нового работника";

        // Список работников
        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private string? _errorMessage;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string UserRoleName => CurrentRole?.Name ?? "Неизвестно";

        public ICommand SaveCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand EditEmployeeCommand { get; }

        public DirectorWindowViewModel(User user, Role role, DatabaseService dbService,
                                       MainWindowViewModel mainWindowViewModel)
            : base(user, role, dbService)
        {
            _mainWindowViewModel = mainWindowViewModel;

            LogoutCommand = new RelayCommand(Logout);
            SaveCommand = new RelayCommand(SaveEmployeeAsync);
            ClearFormCommand = new RelayCommand(ClearForm);
            DeleteCommand = new RelayCommand(DeleteSelectedEmployeeAsync, CanDeleteSelected);
            EditEmployeeCommand = new RelayCommand<Employee>(EditEmployee);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await LoadEmployeesAsync();
            await LoadRolesAsync();
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var list = await DbService.GetEmployeesAsync();
                Employees.Clear();
                foreach (var emp in list)
                    Employees.Add(emp);
            }
            catch { }
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                var roles = await DbService.GetRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                    Roles.Add(role);

                if (Roles.Count > 0)
                    SelectedRole = Roles[0];
            }
            catch { }
        }

        private void EditEmployee(Employee? emp)
        {
            if (emp == null) return;

            IsEditMode = true;
            EditingId = emp.IdEmployee;
            FormTitle = "Редактирование работника";

            FormFullName = emp.FullName;
            FormBirthDate = new DateTimeOffset(emp.BirthDate);
            FormAddress = emp.Address ?? string.Empty;
            FormEducation = emp.Education ?? string.Empty;
            FormQualification = emp.Qualification ?? string.Empty;
            FormOperations = emp.OperationsList ?? string.Empty;

            // При редактировании не меняем логин/пароль/роль
            FormLogin = string.Empty;
            FormPassword = string.Empty;
            SelectedRole = null;
        }

        private async void SaveEmployeeAsync()
        {
            ErrorMessage = null;

            if (!IsEditMode)
            {
                // Создание нового пользователя
                if (string.IsNullOrWhiteSpace(FormLogin) || string.IsNullOrWhiteSpace(FormPassword))
                {
                    ErrorMessage = "Введите логин и пароль";
                    return;
                }

                if (SelectedRole == null)
                {
                    ErrorMessage = "Выберите роль";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(FormFullName))
            {
                ErrorMessage = "Введите ФИО работника";
                return;
            }

            try
            {
                if (IsEditMode)
                {
                    // Редактирование существующего работника
                    var emp = new Employee
                    {
                        IdEmployee = EditingId,
                        FullName = FormFullName,
                        BirthDate = FormBirthDate.DateTime,
                        Address = FormAddress,
                        Education = FormEducation,
                        Qualification = FormQualification,
                        OperationsList = FormOperations
                    };

                    bool success = await DbService.UpdateEmployeeAsync(emp);
                    if (success)
                    {
                        ClearForm();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = "Ошибка при обновлении данных работника";
                    }
                }
                else
                {
                    // Создание нового пользователя и работника
                    var success = await DbService.CreateUserWithEmployeeAsync(
                        FormLogin,
                        FormPassword,
                        SelectedRole!.IdRole,
                        FormFullName,
                        FormBirthDate.DateTime,
                        FormAddress,
                        FormEducation,
                        FormQualification,
                        FormOperations
                    );

                    if (success)
                    {
                        ClearForm();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = "Пользователь с таким логином уже существует";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
            }
        }

        private async void DeleteSelectedEmployeeAsync()
        {
            if (SelectedEmployee != null)
            {
                try
                {
                    await DbService.DeleteEmployeeWithUserAsync(SelectedEmployee.IdEmployee);
                    await LoadDataAsync();
                }
                catch { }
            }
        }

        private bool CanDeleteSelected() => SelectedEmployee != null;

        private void ClearForm()
        {
            IsEditMode = false;
            EditingId = 0;
            FormTitle = "Добавление нового работника";
            FormLogin = string.Empty;
            FormPassword = string.Empty;
            FormFullName = string.Empty;
            FormBirthDate = DateTimeOffset.Now.AddYears(-20);
            FormAddress = string.Empty;
            FormEducation = string.Empty;
            FormQualification = string.Empty;
            FormOperations = string.Empty;
            ErrorMessage = null;

            if (Roles.Count > 0)
                SelectedRole = Roles[0];
        }

        private void Logout()
        {
            App.CurrentUser = null;
            App.CurrentRole = null;
            _mainWindowViewModel.NavigateToLogin();
        }
    }
}