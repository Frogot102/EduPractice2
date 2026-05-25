using CommunityToolkit.Mvvm.ComponentModel;
using EduPractice2.Models;
using EduPractice2.Services;

namespace EduPractice2.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public User CurrentUser { get; }
        public Role CurrentRole { get; }
        protected DatabaseService DbService { get; }

        protected ViewModelBase()
        {
            CurrentUser = null!;
            CurrentRole = null!;
            DbService = null!;
        }

        protected ViewModelBase(User user, Role role, DatabaseService dbService)
        {
            CurrentUser = user;
            CurrentRole = role;
            DbService = dbService;
        }
    }
}