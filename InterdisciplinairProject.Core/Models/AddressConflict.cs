namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a DMX address conflict between two fixtures.
/// </summary>
public class AddressConflict
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressConflict"/> class.
    /// </summary>
    /// <param name="existingFixture">The fixture already in the scene.</param>
    /// <param name="newFixture">The fixture being added.</param>
    /// <param name="overlappingChannels">The list of overlapping DMX channels.</param>
    public AddressConflict(Fixture existingFixture, Fixture newFixture, List<int> overlappingChannels)
    {
        ExistingFixture = existingFixture;
        NewFixture = newFixture;
        OverlappingChannels = overlappingChannels;
    }

    /// <summary>
    /// Gets the fixture that already exists in the scene.
    /// </summary>
    public Fixture ExistingFixture { get; }

    /// <summary>
    /// Gets the fixture that is being added.
    /// </summary>
    public Fixture NewFixture { get; }

    /// <summary>
    /// Gets the list of DMX channels that overlap between the two fixtures.
    /// </summary>
    public List<int> OverlappingChannels { get; }

    /// <summary>
    /// Gets a human-readable description of the conflict.
    /// </summary>
    public string Description =>
        $"Conflict: '{NewFixture.Name}' (channels {NewFixture.StartAddress}-{NewFixture.StartAddress + NewFixture.ChannelCount - 1}) " +
        $"overlaps with '{ExistingFixture.Name}' (channels {ExistingFixture.StartAddress}-{ExistingFixture.StartAddress + ExistingFixture.ChannelCount - 1}) " +
        $"on channels: {string.Join(", ", OverlappingChannels)}";
}
