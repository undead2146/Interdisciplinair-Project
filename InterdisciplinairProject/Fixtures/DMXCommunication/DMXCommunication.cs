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
        public static void SendDMXFrame(string comPort, byte[] data)
        {
            try
            {
                for (int i = 0; i < data.Length; i++)
                    data[i] = (byte)(data[i] % 255);

                using var sp = new SerialPort(comPort, 250000, Parity.None, 8, StopBits.Two);
                sp.Open();
                Thread.Sleep(1);

                sp.BreakState = true;
                Thread.Sleep(1);
                sp.BreakState = false;

                SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(10));

                byte[] frame = new byte[data.Length + 1];
                frame[0] = 0x00;
                Array.Copy(data, 0, frame, 1, data.Length);

                sp.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("DMX ERROR: " + ex.Message);
            }
        }

        // ======================
        // ELO FRAME (SERIAL CABLE)
        // ======================
        public static void SendELOFrame(string comPort, byte[] channelBytes)
        {
            try
            {
                for (int i = 0; i < channelBytes.Length; i++)
                    channelBytes[i] = (byte)(channelBytes[i] % 255);

                using var sp = new SerialPort(comPort, 250000, Parity.None, 8, StopBits.Two)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 100,
                    WriteTimeout = 100
                };
                sp.Open();
                Thread.Sleep(5);

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Pad single-channel sends to at least 10 bytes
                int minLength = 10;
                byte[] padded = new byte[Math.Max(channelBytes.Length, minLength)];
                Array.Copy(channelBytes, padded, channelBytes.Length);

                byte[] frame = new byte[start.Length + padded.Length + stop.Length];
                Array.Copy(start, 0, frame, 0, start.Length);
                Array.Copy(padded, 0, frame, start.Length, padded.Length);
                //Array.Copy(stop, 0, frame, start.Length + padded.Length, stop.Length);

                sp.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("ELO ERROR: " + ex.Message);
            }
        }

        // ======================
        // ELO FRAME (WiFi / TCP)
        // ======================
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
