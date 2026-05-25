using Avalonia.Controls;
using EduPractice2.ViewModels;

namespace EduPractice2.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow(RegisterViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}