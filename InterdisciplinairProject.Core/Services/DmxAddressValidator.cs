using System.Diagnostics;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Services;

/// <summary>
/// Service for validating DMX addresses of fixtures to detect conflicts.
/// </summary>
public class DmxAddressValidator : IDmxAddressValidator
{
    /// <summary>
    /// The maximum number of channels in a DMX universe.
    /// </summary>
    public const int MaxDmxChannel = 512;

    /// <summary>
    /// The minimum valid DMX address.
    /// </summary>
    public const int MinDmxChannel = 1;

    /// <inheritdoc/>
    public AddressValidationResult ValidateFixtureAddress(Fixture fixtureToAdd, IEnumerable<Fixture> existingFixtures)
    {
        if (fixtureToAdd == null)
        {
            throw new ArgumentNullException(nameof(fixtureToAdd));
        }

        return ValidateFixtureAtAddress(fixtureToAdd, fixtureToAdd.StartAddress, existingFixtures);
    }

    /// <inheritdoc/>
    public AddressValidationResult ValidateFixtureAtAddress(Fixture fixtureToAdd, int startAddress, IEnumerable<Fixture> existingFixtures)
    {
        if (fixtureToAdd == null)
        {
            throw new ArgumentNullException(nameof(fixtureToAdd));
        }

        var result = new AddressValidationResult();
        var fixtures = existingFixtures?.ToList() ?? new List<Fixture>();
        var channelCount = fixtureToAdd.ChannelCount;

        // Validate start address is within valid range
        if (startAddress < MinDmxChannel)
        {
            result.Warnings.Add($"Start address {startAddress} is below minimum ({MinDmxChannel}). Using {MinDmxChannel} instead.");
            startAddress = MinDmxChannel;
        }

        // Calculate end address
        var endAddress = startAddress + channelCount - 1;

        // Check if fixture exceeds DMX universe
        if (endAddress > MaxDmxChannel)
        {
            result.ExceedsDmxUniverse = true;
            Debug.WriteLine($"[DEBUG] DmxAddressValidator: Fixture '{fixtureToAdd.Name}' exceeds DMX universe (end address: {endAddress})");
        }

        // Check for conflicts with existing fixtures
        foreach (var existingFixture in fixtures)
        {
            // Skip if comparing against the same fixture instance
            if (!string.IsNullOrEmpty(fixtureToAdd.InstanceId) &&
                fixtureToAdd.InstanceId == existingFixture.InstanceId)
            {
                continue;
            }

            var existingStart = existingFixture.StartAddress;
            var existingEnd = existingStart + existingFixture.ChannelCount - 1;

            // Check for overlap
            if (startAddress <= existingEnd && endAddress >= existingStart)
            {
                // Calculate overlapping channels
                var overlapStart = Math.Max(startAddress, existingStart);
                var overlapEnd = Math.Min(endAddress, existingEnd);
                var overlappingChannels = Enumerable.Range(overlapStart, overlapEnd - overlapStart + 1).ToList();

                var conflict = new AddressConflict(existingFixture, fixtureToAdd, overlappingChannels);
                result.Conflicts.Add(conflict);

                Debug.WriteLine($"[DEBUG] DmxAddressValidator: Conflict detected - {conflict.Description}");
            }
        }

        // If there are conflicts, suggest the next available address
        if (result.Conflicts.Count > 0 || result.ExceedsDmxUniverse)
        {
            var suggestedAddress = FindNextAvailableAddress(channelCount, fixtures);
            if (suggestedAddress > 0)
            {
                result.SuggestedStartAddress = suggestedAddress;
                Debug.WriteLine($"[DEBUG] DmxAddressValidator: Suggested start address: {suggestedAddress}");
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public int FindNextAvailableAddress(int channelCount, IEnumerable<Fixture> existingFixtures)
    {
        if (channelCount <= 0)
        {
            return MinDmxChannel;
        }

        var fixtures = existingFixtures?.ToList() ?? new List<Fixture>();

        if (fixtures.Count == 0)
        {
            return MinDmxChannel;
        }

        // Build a list of occupied channel ranges, sorted by start address
        var occupiedRanges = fixtures
            .Where(f => f.ChannelCount > 0)
            .Select(f => new { Start = f.StartAddress, End = f.StartAddress + f.ChannelCount - 1 })
            .OrderBy(r => r.Start)
            .ToList();

        // Check if there's space at the beginning
        if (occupiedRanges.Count == 0 || occupiedRanges[0].Start > channelCount)
        {
            return MinDmxChannel;
        }

        // Look for gaps between occupied ranges
        for (int i = 0; i < occupiedRanges.Count - 1; i++)
        {
            var gapStart = occupiedRanges[i].End + 1;
            var gapEnd = occupiedRanges[i + 1].Start - 1;
            var gapSize = gapEnd - gapStart + 1;

            if (gapSize >= channelCount)
            {
                return gapStart;
            }
        }

        // Check if there's space after the last fixture
        var lastEnd = occupiedRanges.Last().End;
        var nextAddress = lastEnd + 1;

        if (nextAddress + channelCount - 1 <= MaxDmxChannel)
        {
            return nextAddress;
        }

        // No space available in the DMX universe
        Debug.WriteLine($"[DEBUG] DmxAddressValidator: No available address found for {channelCount} channels");
        return -1;
    }

    /// <inheritdoc/>
    public List<AddressConflict> GetAllConflicts(IEnumerable<Fixture> fixtures)
    {
        var conflicts = new List<AddressConflict>();
        var fixtureList = fixtures?.ToList() ?? new List<Fixture>();

        for (int i = 0; i < fixtureList.Count; i++)
        {
            for (int j = i + 1; j < fixtureList.Count; j++)
            {
                var fixture1 = fixtureList[i];
                var fixture2 = fixtureList[j];

                var start1 = fixture1.StartAddress;
                var end1 = start1 + fixture1.ChannelCount - 1;
                var start2 = fixture2.StartAddress;
                var end2 = start2 + fixture2.ChannelCount - 1;

                // Check for overlap
                if (start1 <= end2 && end1 >= start2)
                {
                    var overlapStart = Math.Max(start1, start2);
                    var overlapEnd = Math.Min(end1, end2);
                    var overlappingChannels = Enumerable.Range(overlapStart, overlapEnd - overlapStart + 1).ToList();

                    conflicts.Add(new AddressConflict(fixture1, fixture2, overlappingChannels));
                }
            }
        }

        return conflicts;
    }

    /// <inheritdoc/>
    public bool IsChannelInUse(int channel, IEnumerable<Fixture> existingFixtures, string? excludeFixtureId = null)
    {
        if (channel < MinDmxChannel || channel > MaxDmxChannel)
        {
            return false;
        }

        return GetFixtureAtChannel(channel, existingFixtures, excludeFixtureId) != null;
    }

    /// <inheritdoc/>
    public Fixture? GetFixtureAtChannel(int channel, IEnumerable<Fixture> existingFixtures)
    {
        return GetFixtureAtChannel(channel, existingFixtures, null);
    }

    /// <summary>
    /// Gets the fixture occupying a specific DMX channel, optionally excluding a specific fixture.
    /// </summary>
    /// <param name="channel">The DMX channel to check (1-512).</param>
    /// <param name="existingFixtures">The fixtures to check.</param>
    /// <param name="excludeFixtureId">Optional fixture instance ID to exclude from the check.</param>
    /// <returns>The fixture using the channel, or null if the channel is free.</returns>
    private Fixture? GetFixtureAtChannel(int channel, IEnumerable<Fixture> existingFixtures, string? excludeFixtureId)
    {
        if (channel < MinDmxChannel || channel > MaxDmxChannel)
        {
            return null;
        }

        var fixtures = existingFixtures ?? Enumerable.Empty<Fixture>();

        foreach (var fixture in fixtures)
        {
            // Skip excluded fixture
            if (!string.IsNullOrEmpty(excludeFixtureId) && fixture.InstanceId == excludeFixtureId)
            {
                continue;
            }

            var start = fixture.StartAddress;
            var end = start + fixture.ChannelCount - 1;

            if (channel >= start && channel <= end)
            {
                return fixture;
            }
        }

        return null;
    }
}
