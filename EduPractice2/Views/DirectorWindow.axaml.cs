using Avalonia.Controls;
using EduPractice2.ViewModels;

namespace EduPractice2.Views
{
    public partial class DirectorWindow : Window
    {
        public DirectorWindow(DirectorWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}