using System.Windows;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for SceneNameDialog.xaml.
/// A modern dialog for entering or editing scene names.
/// </summary>
public partial class SceneNameDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SceneNameDialog"/> class.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="prompt">The prompt text to display.</param>
    /// <param name="defaultValue">The default value to populate in the textbox.</param>
    public SceneNameDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultValue;
        InputTextBox.SelectAll();
        InputTextBox.Focus();
    }

    /// <summary>
    /// Gets the text entered by the user.
    /// </summary>
    /// <value>
    /// The current text in the input text box.
    /// </value>
    public string? InputText => InputTextBox.Text;

    /// <summary>
    /// Handles the OK button click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            MessageBox.Show("Scène naam mag niet leeg zijn.", "Validatie", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the Cancel button click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
