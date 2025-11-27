using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for validating DMX addresses of fixtures to detect conflicts.
/// </summary>
public interface IDmxAddressValidator
{
    /// <summary>
    /// Validates whether a fixture can be added to a scene without address conflicts.
    /// </summary>
    /// <param name="fixtureToAdd">The fixture to be added.</param>
    /// <param name="existingFixtures">The fixtures already in the scene.</param>
    /// <returns>A validation result containing any conflicts found.</returns>
    AddressValidationResult ValidateFixtureAddress(Fixture fixtureToAdd, IEnumerable<Fixture> existingFixtures);

    /// <summary>
    /// Validates whether a fixture can be added to a scene at a specific start address.
    /// </summary>
    /// <param name="fixtureToAdd">The fixture to be added.</param>
    /// <param name="startAddress">The proposed start address.</param>
    /// <param name="existingFixtures">The fixtures already in the scene.</param>
    /// <returns>A validation result containing any conflicts found.</returns>
    AddressValidationResult ValidateFixtureAtAddress(Fixture fixtureToAdd, int startAddress, IEnumerable<Fixture> existingFixtures);

    /// <summary>
    /// Finds the next available start address for a fixture in a scene.
    /// </summary>
    /// <param name="channelCount">The number of channels the fixture needs.</param>
    /// <param name="existingFixtures">The fixtures already in the scene.</param>
    /// <returns>The next available start address, or -1 if no space is available in the DMX universe.</returns>
    int FindNextAvailableAddress(int channelCount, IEnumerable<Fixture> existingFixtures);

    /// <summary>
    /// Gets all conflicts between fixtures in a scene.
    /// </summary>
    /// <param name="fixtures">The fixtures to check for conflicts.</param>
    /// <returns>A list of all address conflicts found.</returns>
    List<AddressConflict> GetAllConflicts(IEnumerable<Fixture> fixtures);

    /// <summary>
    /// Checks if a specific DMX channel is already in use by any fixture.
    /// </summary>
    /// <param name="channel">The DMX channel to check (1-512).</param>
    /// <param name="existingFixtures">The fixtures to check against.</param>
    /// <param name="excludeFixtureId">Optional fixture instance ID to exclude from the check.</param>
    /// <returns>True if the channel is in use; otherwise, false.</returns>
    bool IsChannelInUse(int channel, IEnumerable<Fixture> existingFixtures, string? excludeFixtureId = null);

    /// <summary>
    /// Gets the fixture occupying a specific DMX channel.
    /// </summary>
    /// <param name="channel">The DMX channel to check (1-512).</param>
    /// <param name="existingFixtures">The fixtures to check.</param>
    /// <returns>The fixture using the channel, or null if the channel is free.</returns>
    Fixture? GetFixtureAtChannel(int channel, IEnumerable<Fixture> existingFixtures);
}
