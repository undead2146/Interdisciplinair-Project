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
                throw new Exception("ELO CABLE ERROR: Data must contain at least 1 byte");
            if (data.Length > 256)
                throw new Exception("ELO CABLE ERROR: Data length exceeds 256 bytes");

            // Build ELO frame
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

            try
            {
                using var sp = new SerialPort(serialPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    DtrEnable = true,
                    RtsEnable = true,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                sp.Open();

                // === Reset adapter ===
                sp.DtrEnable = false;
                sp.RtsEnable = false;
                Thread.Sleep(5);

                sp.DtrEnable = true;
                sp.RtsEnable = true;
                Thread.Sleep(5);
                sp.DtrEnable = false;
                sp.RtsEnable = false;
                Thread.Sleep(5);

                // === Send frame byte by byte ===
                foreach (byte b in frame)
                {
                    sp.Write(new byte[] { b }, 0, 1);
                    Thread.Sleep(1); // 1ms between bytes
                }

                sp.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                throw new Exception("ELO CABLE ERROR: " + ex.Message);
            }
        }


        // ======================
        // ELO FRAME (WiFi)
        // ======================
        public static void SendELOWifiFrame(string ipAddress, int port, byte[] data)
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
                throw new Exception($"ELO WIFI ERROR: Could not connect to {ipAddress}:{port} — {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception("ELO WIFI ERROR: " + ex.Message);
            }
        }
    }
}
