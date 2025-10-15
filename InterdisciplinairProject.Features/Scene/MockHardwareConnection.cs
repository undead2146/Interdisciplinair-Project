using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;

namespace InterdisciplinairProject.Features.Scene;

/// <summary>
/// Mock implementation of IHardwareConnection for testing purposes.
/// Logs all DMX values sent.
/// </summary>
public class MockHardwareConnection : IHardwareConnection
{
    private readonly Dictionary<(int Universe, int Channel), byte> _dmxValues = new();

    /// <summary>
    /// Sends a DMX value and logs it.
    /// </summary>
    public Task SendDmxValueAsync(int universe, int channel, byte value)
    {
        _dmxValues[(universe, channel)] = value;
        Console.WriteLine($"U{universe}:Ch{channel} = {value}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current DMX value.
    /// </summary>
    public byte GetDmxValue(int universe, int channel)
    {
        return _dmxValues.TryGetValue((universe, channel), out var value) ? value : (byte)0;
    }
}
