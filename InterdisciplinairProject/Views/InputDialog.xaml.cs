using System.Windows;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for InputDialog.xaml.
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputDialog"/> class.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="prompt">The prompt text to display.</param>
    public InputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
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
