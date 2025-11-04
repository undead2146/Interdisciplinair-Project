using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;

namespace InterdisciplinairProject.Core.Repositories;

/// <summary>
/// Dummy implementation of IHardwareConnection for testing purposes.
/// </summary>
public class DummyHardwareConnection : IHardwareConnection
{
    public Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value)
    {
        System.Diagnostics.Debug.WriteLine($"[DUMMY] Setting {fixtureInstanceId}.{channelName} to {value}");
        return Task.FromResult(true);
    }
}