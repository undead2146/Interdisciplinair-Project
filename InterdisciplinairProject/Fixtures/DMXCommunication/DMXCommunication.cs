using System;
using System.IO.Ports;
using System.Net.Sockets;
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
        /// <param name="serialPort">The COM port name (e.g., "COM3").</param>
        /// <param name="universe">The DMX channel data (up to 512 bytes).</param>
        public static void SendDMXFrame(string serialPort, byte[] universe)
        {
            try
            {
                using var sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two);
                sp.Open();

                // Break + start code
                Thread.Sleep(1);
                sp.BreakState = true;
                Thread.Sleep(1);
                sp.BreakState = false;

                SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(10));

                // DMX frame: start code + universe
                byte[] frame = new byte[universe.Length + 1];
                frame[0] = 0x00;
                Array.Copy(universe, 0, frame, 1, universe.Length);

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
        /// <param name="serialPort">The COM port name (e.g., "COM3").</param>
        /// <param name="universe">The channel data bytes to send.</param>
        public static void SendELOFrame(string serialPort, byte[] universe)
        {
            try
            {
                using var sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 100,
                    WriteTimeout = 100,
                };
                sp.Open();

                Thread.Sleep(5);

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Pad data to at least 10 bytes for single-channel sends
                int minLength = 10;
                byte[] padded = new byte[Math.Max(universe.Length, minLength)];
                Array.Copy(universe, padded, universe.Length);

                // Build complete frame: START + PADDED DATA + STOP
                byte[] frame = new byte[start.Length + padded.Length + stop.Length];
                Array.Copy(start, 0, frame, 0, start.Length);
                Array.Copy(padded, 0, frame, start.Length, padded.Length);
                Array.Copy(stop, 0, frame, start.Length + padded.Length, stop.Length);

                sp.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("ELO ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends an ELO frame over WiFi/TCP to the specified IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the DMX controller.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <param name="channelBytes">The channel data bytes to send.</param>
        public static void SendELOWifiFrame(string ipAddress, int port, byte[] channelBytes)
        {
            try
            {
                for (int i = 0; i < channelBytes.Length; i++)
                    channelBytes[i] = (byte)(channelBytes[i] % 255);

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Pad data to at least 10 bytes for single-channel sends
                int minLength = 10;
                byte[] padded = new byte[Math.Max(channelBytes.Length, minLength)];
                Array.Copy(channelBytes, padded, channelBytes.Length);

                byte[] frame = new byte[start.Length + padded.Length + stop.Length];
                Array.Copy(start, 0, frame, 0, start.Length);
                Array.Copy(padded, 0, frame, start.Length, padded.Length);
                Array.Copy(stop, 0, frame, start.Length + padded.Length, stop.Length);

                using TcpClient client = new TcpClient();
                client.ReceiveTimeout = 500;
                client.SendTimeout = 500;

                client.Connect(ipAddress, port);

                using NetworkStream stream = client.GetStream();
                stream.Write(frame, 0, frame.Length);
                stream.Flush();
            }
            catch (SocketException ex)
            {
                throw new Exception($"ELO WIFI ERROR: Could not connect to {ipAddress}:{port} — {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception("ELO WIFI ERROR: " + ex.Message);
            }
        }
    }
}
