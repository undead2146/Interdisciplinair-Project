using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for hardware connection operations.
/// </summary>
public interface IHardwareConnection
{
    /// <summary>
    /// Sends a value to a specific channel of a fixture.
    /// </summary>
    /// <param name="fixtureInstanceId">The instance ID of the fixture.</param>
    /// <param name="channelName">The name of the channel (e.g. "dimmer").</param>
    /// <param name="value">The value between 0 and 255.</param>
    /// <returns>True if successful, otherwise false.</returns>
    Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value);
}
