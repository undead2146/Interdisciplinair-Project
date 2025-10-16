using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for FixtureContentView.xaml
    /// </summary>
    public partial class FixtureContentView : UserControl
    {
        public event EventHandler? BackRequested;
        public event EventHandler? EditRequested;

        public FixtureContentView()
        {
            InitializeComponent();
        }

        public void LoadFixtureContentFromJson(string json) 
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try 
            {
            }
            catch (JsonException ex) 
            {
                MessageBox.Show("Json kan niet worden geladen: {ex.Message }", "Fout");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) 
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
