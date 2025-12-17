using System.IO.Ports;
using System.Net.Sockets;

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
        public static void SendELOFrame(string serialPort, byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    throw new Exception("ELO ERROR: Data must contain at least 1 byte");

                if (data.Length > 256)
                    throw new Exception("ELO ERROR: Data length exceeds protocol limit (256)");

                // ELO protocol bytes
                byte[] start = { 0x00, 0xFF, 0x00 };

                //byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Length = data bytes + mandatory zero byte - 1
                byte length = (byte)data.Length;

                // Frame format:
                // [START][LEN][0x00][DATA][STOP]
                byte[] frame = new byte[
                    start.Length +
                    1 + // length
                    1 + // zero byte
                    data.Length
                    //+ stop.Length
                ];

                int offset = 0;

                // Start bytes
                Array.Copy(start, 0, frame, offset, start.Length);
                offset += start.Length;

                // Length byte
                frame[offset++] = length;

                // Mandatory zero byte
                frame[offset++] = 0x00;

                // Data payload
                Array.Copy(data, 0, frame, offset, data.Length);
                offset += data.Length;

                // Stop bytes
                // Array.Copy(stop, 0, frame, offset, stop.Length);
                using SerialPort sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 200,
                    WriteTimeout = 200,
                };

                sp.Open();
                Thread.Sleep(5); // allow UART to stabilize
                sp.Write(frame, 0, frame.Length);
                sp.BaseStream.Flush();
                sp.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("ELO CABLE ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends an ELO frame over WiFi/TCP to the specified IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the DMX controller.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <param name="channelBytes">The channel data bytes to send.</param>
        public static void SendELOWifiFrame(string ipAddress, int port, byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    throw new Exception("ELO WIFI ERROR: Data must contain at least 1 byte");
                if (data.Length > 512)
                    throw new Exception("ELO WIFI ERROR: Data length exceeds protocol limit (512)");

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };
                byte len = (byte)(data.Length - 1);

                byte[] frame = new byte[start.Length + 1 + data.Length + stop.Length];
                int offset = 0;

                Array.Copy(start, 0, frame, offset, start.Length);
                offset += start.Length;

                frame[offset++] = len;
                Array.Copy(data, 0, frame, offset, data.Length);
                offset += data.Length;

                Array.Copy(stop, 0, frame, offset, stop.Length);

                using TcpClient client = new TcpClient
                {
                    ReceiveTimeout = 500,
                    SendTimeout = 500,
                };
                client.Connect(ipAddress, port);
                using NetworkStream stream = client.GetStream();
                stream.Write(frame, 0, frame.Length);
                stream.Flush();

                // DebugELOFrame("WIFI", data, frame); // commented for now
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
