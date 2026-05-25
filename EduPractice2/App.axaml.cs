using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EduPractice2.Models;
using EduPractice2.Services;
using EduPractice2.ViewModels;
using EduPractice2.Views;

namespace EduPractice2
{
    public partial class App : Application
    {
        public static User? CurrentUser { get; set; }
        public static Role? CurrentRole { get; set; }

        private static DatabaseService? _dbService;

        public static DatabaseService DbService
        {
            get
            {
                if (_dbService == null)
                {
                    var connectionString = "Host=localhost;Port=5432;Database=EducationPracticeCompany;Username=postgres;Password=frogot1";
                    _dbService = new DatabaseService(connectionString);
                }
                return _dbService;
            }
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow(new MainWindowViewModel(DbService));
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}