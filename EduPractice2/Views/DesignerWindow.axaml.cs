using Avalonia.Controls;
using EduPractice2.ViewModels;

namespace EduPractice2.Views
{
    public partial class DesignerWindow : Window
    {
        public DesignerWindow(DesignerWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.GetType().GetField("_window")?.SetValue(viewModel, this);
        }
    }
}