using System.Threading.Tasks;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for hardware connections to DMX devices.
/// </summary>
public interface IHardwareConnection
{
    /// <summary>
    /// Sends a DMX value to a specific universe and channel.
    /// </summary>
    /// <param name="universe">The DMX universe (1-512).</param>
    /// <param name="channel">The DMX channel (1-512).</param>
    /// <param name="value">The value to send (0-255).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendDmxValueAsync(int universe, int channel, byte value);

    /// <summary>
    /// Gets the current DMX value for a specific universe and channel.
    /// </summary>
    /// <param name="universe">The DMX universe.</param>
    /// <param name="channel">The DMX channel.</param>
    /// <returns>The current value.</returns>
    byte GetDmxValue(int universe, int channel);
}
