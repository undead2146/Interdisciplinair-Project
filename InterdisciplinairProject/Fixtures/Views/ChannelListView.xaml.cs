using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for ChannelListView.xaml
    /// </summary>
    public partial class ChannelListView : Window
    {
        public ChannelListView()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = FixtureNameTextBox.Text;

            if (string.IsNullOrEmpty(name)) 
            {
                MessageBox.Show("Please fill in a valid name");
                return;
            }

            // map 'data' aanmaken
            string dataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);

            // file name
            string safeName = string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string filePath = System.IO.Path.Combine(dataDir, safeName + ".json");

            //checken of bestand al bestaat
            if (File.Exists(filePath)) 
            {
                MessageBox.Show("There already exists a fixture with this name");
                return;
            }

            // Root JSON-object
            var root = new JsonObject
            {
                ["name"] = FixtureNameTextBox.Text ?? string.Empty
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            try
            {
                File.WriteAllText(filePath, json);
                MessageBox.Show($"Fixture is saved succesfully");
                this.Close();
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error with saving fixture: {ioEx.Message}");
                this.Close();
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
            messageBoxText: "Are you sure you want to cancel making this fixture?",
            caption: "Confirm Cancel",
            button: MessageBoxButton.YesNo,
            icon: MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                //MessageBox.Show("fixture canceled\n\r");
                this.Close();

            }
            else
            {
                //do nothing
            }
        }
    }
}
