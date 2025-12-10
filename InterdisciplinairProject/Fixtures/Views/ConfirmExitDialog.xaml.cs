using System.Windows;
using System;
using InterdisciplinairProject.Fixtures.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class ConfirmExitDialog : Window
    {
        public UnsavedChangesAction ResultAction { get; private set; }

        public ConfirmExitDialog(string manufacturerName)
        {
            InitializeComponent();

            MessageTextBlock.Text = $"Changes to manufacturer '{manufacturerName}' have not been saved. What do you want to do?";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = UnsavedChangesAction.SaveAndContinue;
            DialogResult = true;
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = UnsavedChangesAction.DiscardAndContinue;
            DialogResult = true;
        }

        private void ContinueEditingButton_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = UnsavedChangesAction.ContinueEditing;
            DialogResult = false;
        }
    }
}