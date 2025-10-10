using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Core.Interfaces
{
    public interface IHardwareConnection
    {
        /// <summary>
        /// Stuurt een waarde naar een specifiek kanaal van een fixture
        /// </summary>
        /// <param name="fixtureInstanceId">De instance ID van de fixture</param>
        /// <param name="channelName">De naam van het kanaal (bijv. "dimmer")</param>
        /// <param name="value">De waarde tussen 0 en 255</param>
        /// <returns>True als het succesvol is, anders false</returns>
        Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value);

    }
}
