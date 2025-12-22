using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.Communication
{
    public static class DMXCommunication
    {
        // ======================
        // STANDARD DMX FRAME (SERIAL)
        // ======================
        public static void SendDMXFrame(string serialPort, byte[] universe)
        {
            try
            {
                using var sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two);
                sp.Open();

                Thread.Sleep(1);
                sp.BreakState = true;
                Thread.Sleep(1);
                sp.BreakState = false;

                SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(10));

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

        // ======================
        // ELO FRAME (USB / SERIAL)
        // ======================
        public static void SendELOFrame(string serialPort, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new Exception("ELO USB ERROR: Data must contain at least 1 byte");

            // Payload = ZERO byte + data
            // Len counts starting from ZERO byte
            if (data.Length + 1 > 255)
                throw new Exception("ELO USB ERROR: Payload length exceeds 255 bytes");

            // Start sequence
            byte[] start = { 0x00, 0xFF, 0x00 };

            // Build payload
            byte[] payload = new byte[1 + data.Length];
            payload[0] = 0x00;                  // ZERO byte
            Array.Copy(data, 0, payload, 1, data.Length);

            byte len = (byte)payload.Length;

            // Build final frame
            byte[] frame = new byte[start.Length + 1 + payload.Length];
            int offset = 0;

            Array.Copy(start, 0, frame, offset, start.Length);
            offset += start.Length;

            frame[offset++] = len;

            Array.Copy(payload, 0, frame, offset, payload.Length);

            try
            {
                using var sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                sp.Open();
                sp.Write(frame, 0, frame.Length);
                sp.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                throw new Exception("ELO USB ERROR: " + ex.Message);
            }
        }



        // ======================
        // ELO FRAME (WiFi)
        // ======================
        public static void SendELOWifiFrame(string ipAddress, int port, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new Exception("ELO WIFI ERROR: Data must contain at least 1 byte");

            // Payload = ZERO byte + data
            // Len counts starting from ZERO byte
            if (data.Length + 1 > 255)
                throw new Exception("ELO WIFI ERROR: Payload length exceeds 255 bytes");

            byte[] start = { 0x00, 0xFF, 0x00 };
            byte[] stop = { 0xFF, 0xF0, 0xF0 };

            // Build payload
            byte[] payload = new byte[1 + data.Length];
            payload[0] = 0x00;                 // ZERO byte
            Array.Copy(data, 0, payload, 1, data.Length);

            byte len = (byte)payload.Length;

            // Build final frame
            byte[] frame = new byte[start.Length + 1 + payload.Length + stop.Length];
            int offset = 0;

            Array.Copy(start, 0, frame, offset, start.Length);
            offset += start.Length;

            frame[offset++] = len;

            Array.Copy(payload, 0, frame, offset, payload.Length);
            offset += payload.Length;

            Array.Copy(stop, 0, frame, offset, stop.Length);

            try
            {
                using TcpClient client = new TcpClient
                {
                    ReceiveTimeout = 500,
                    SendTimeout = 500
                };

                client.Connect(ipAddress, port);

                using NetworkStream stream = client.GetStream();
                stream.Write(frame, 0, frame.Length);
                stream.Flush();
            }
            catch (SocketException ex)
            {
                throw new Exception(
                    $"ELO WIFI ERROR: Could not connect to {ipAddress}:{port} — {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                throw new Exception("ELO WIFI ERROR: " + ex.Message);
            }
        }

    }
}
