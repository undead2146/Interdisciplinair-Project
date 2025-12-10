namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for DMX communication services.
/// Defines the contract for managing a 512-channel DMX universe and transmitting
/// DMX frames to hardware controllers via serial communication.
/// </summary>
public interface IDmxService
{
    /// <summary>
    /// Gets or sets the COM port used for DMX communication.
    /// </summary>
    string? ComPort { get; set; }

    /// <summary>
    /// Sets a single DMX channel value.
    /// Updates the internal universe state without transmitting to hardware.
    /// </summary>
    /// <param name="address">The DMX address (1-512).</param>
    /// <param name="value">The value (0-255).</param>
    void SetChannel(int address, byte value);

    /// <summary>
    /// Gets the current value of a DMX channel.
    /// </summary>
    /// <param name="address">The DMX address (1-512).</param>
    /// <returns>The current value (0-255).</returns>
    byte GetChannel(int address);

    /// <summary>
    /// Sends the current DMX universe to the controller.
    /// Transmits all 512 channels to the DMX hardware.
    /// </summary>
    /// <returns>True if successful, otherwise false.</returns>
    bool SendFrame();

    /// <summary>
    /// Clears all DMX channels to zero.
    /// Resets the entire universe state.
    /// </summary>
    void ClearAllChannels();
}
