using System.Text.Json.Serialization;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.Models;

/// <summary>
/// Represents a channel in a fixture for UI binding.
/// </summary>
public class Channel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Channel"/> class.
    /// </summary>
    public Channel()
    {
        Name = string.Empty;
        Type = string.Empty;
    }

    /// <summary>
    /// Gets or sets the name of the channel.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the value of the channel.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the test command for the channel.
    /// </summary>
    [JsonIgnore]
    public ICommand? TestCommand { get; set; }

    /// <summary>
    /// Gets or sets the numeric parameter value.
    /// </summary>
    [JsonIgnore]
    public int Parameter
    {
        get => int.TryParse(Value, out int val) ? val : 0;
        set => Value = value.ToString();
    }
}
