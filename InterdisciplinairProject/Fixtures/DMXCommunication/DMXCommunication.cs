using System;
using System.IO.Ports;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.Communication
{
    /// <summary>
    /// Provides low-level DMX communication functionality for sending DMX frames to controllers.
    /// </summary>
    public static class DMXCommunication
    {
        /// <summary>
        /// Sends a standard DMX512 frame to the specified COM port.
        /// </summary>
        /// <param name="comPort">The COM port name (e.g., "COM3").</param>
        /// <param name="data">The DMX channel data (up to 512 bytes).</param>
        public static void SendDMXFrame(string comPort, byte[] data)
        {
            try
            {
                // Ensure all values are 0–255 using modulo 255
                for (int i = 0; i < data.Length; i++)
                    data[i] = (byte)(data[i] % 255);

                using var sp = new SerialPort(comPort, 250000, Parity.None, 8, StopBits.Two);
                sp.Open();
                Thread.Sleep(1); // small delay

                // BREAK
                sp.BreakState = true;
                Thread.Sleep(1);
                sp.BreakState = false;

                // MAB
                SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(10));

                // STARTCODE + DATA
                byte[] frame = new byte[data.Length + 1];
                frame[0] = 0x00; // STARTCODE
                Array.Copy(data, 0, frame, 1, data.Length);

                sp.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("DMX ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends an ELO (Cable) frame to the specified COM port.
        /// </summary>
        /// <param name="comPort">The COM port name (e.g., "COM3").</param>
        /// <param name="channelBytes">The channel data bytes to send.</param>
        public static void SendELOFrame(string comPort, byte[] channelBytes)
        {
            try
            {
                // Byte values are already constrained to 0-255, no modification needed
                using var sp = new SerialPort(comPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 100,
                    WriteTimeout = 100,
                };
                sp.Open();

                // Give adapter time to settle
                Thread.Sleep(20);

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Build complete frame: START + DATA + STOP
                byte[] frame = new byte[start.Length + channelBytes.Length];
                Array.Copy(start, 0, frame, 0, start.Length);
                Array.Copy(channelBytes, 0, frame, start.Length, channelBytes.Length);

                // Array.Copy(stop, 0, frame, start.Length + channelBytes.Length, stop.Length);

                // Send everything in ONE WRITE
                sp.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("ELO ERROR: " + ex.Message);
            }
        }
    }
}
