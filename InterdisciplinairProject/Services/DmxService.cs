using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Fixtures.Communication;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Provides DMX communication services including COM port management and frame transmission.
/// This service maintains the state of a complete 512-channel DMX universe and handles
/// low-level communication with DMX controllers via serial port.
/// Key responsibilities:
/// - Maintains a 512-byte array representing the complete DMX universe state.
/// - Automatically discovers available COM ports for DMX communication.
/// - Provides methods to update individual channels without affecting others.
/// - Sends complete DMX frames to the controller preserving all channel states.
/// This allows multiple fixtures to coexist in the same universe with independent control.
/// </summary>
public class DmxService : IDmxService
{
    private const int DmxUniverseSize = 512;
    private readonly byte[] _dmxUniverse;
    private string? _comPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="DmxService"/> class.
    /// </summary>
    public DmxService()
    {
        _dmxUniverse = new byte[DmxUniverseSize];
        _comPort = DiscoverComPort();

        if (_comPort != null)
        {
            Debug.WriteLine($"[DMX] COM port discovered: {_comPort}");
        }
        else
        {
            Debug.WriteLine("[DMX] No COM port found");
        }
    }

    /// <summary>
    /// Gets or sets the COM port used for DMX communication.
    /// </summary>
    public string? ComPort
    {
        get => _comPort;
        set => _comPort = value;
    }

    /// <summary>
    /// Sets a single DMX channel value.
    /// Updates the internal DMX universe state without sending to the controller.
    /// Call SendFrame() to transmit the updated universe to the DMX hardware.
    /// This allows multiple channels to be updated before sending a single frame.
    /// </summary>
    /// <param name="address">The DMX address (1-512).</param>
    /// <param name="value">The value (0-255).</param>
    public void SetChannel(int address, byte value)
    {
        if (address < 1 || address > DmxUniverseSize)
        {
            throw new ArgumentOutOfRangeException(nameof(address), $"DMX address must be between 1 and {DmxUniverseSize}");
        }

        _dmxUniverse[address - 1] = value;
        Debug.WriteLine($"[DMX] Set channel {address} = {value}");
    }

    /// <summary>
    /// Gets the current value of a DMX channel.
    /// </summary>
    /// <param name="address">The DMX address (1-512).</param>
    /// <returns>The current value (0-255).</returns>
    public byte GetChannel(int address)
    {
        if (address < 1 || address > DmxUniverseSize)
        {
            throw new ArgumentOutOfRangeException(nameof(address), $"DMX address must be between 1 and {DmxUniverseSize}");
        }

        return _dmxUniverse[address - 1];
    }

    /// <summary>
    /// Sends the current DMX universe to the controller.
    /// Transmits all 512 channels via the configured COM port using standard DMX512 protocol.
    /// The DMXCommunication class handles the low-level serial communication including
    /// break, mark-after-break, start code, and data transmission.
    /// </summary>
    /// <returns>True if successful, otherwise false.</returns>
    public bool SendFrame()
    {
        if (string.IsNullOrEmpty(_comPort))
        {
            Debug.WriteLine("[DMX] Cannot send frame: No COM port configured");
            return false;
        }

        try
        {
            Debug.WriteLine($"[DMX] Sending frame to {_comPort}");
            DMXCommunication.SendDMXFrame(_comPort, _dmxUniverse);
            Debug.WriteLine("[DMX] Frame sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DMX] Error sending frame: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears all DMX channels to zero.
    /// </summary>
    public void ClearAllChannels()
    {
        Array.Clear(_dmxUniverse, 0, _dmxUniverse.Length);
        Debug.WriteLine("[DMX] All channels cleared");
    }

    /// <summary>
    /// Discovers an available COM port by checking all available serial ports.
    /// </summary>
    /// <returns>The name of the discovered COM port, or null if none found.</returns>
    private static string? DiscoverComPort()
    {
        var portNames = SerialPort.GetPortNames();

        foreach (var port in portNames)
        {
            try
            {
                using var sp = new SerialPort(port);
                sp.Open();
                sp.Close();
                Debug.WriteLine($"[DMX] Found available COM port: {port}");
                return port;
            }
            catch
            {
                Debug.WriteLine($"[DMX] COM port {port} is not available");
            }
        }

        return null;
    }
}
