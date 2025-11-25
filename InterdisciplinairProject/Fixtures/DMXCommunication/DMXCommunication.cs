using System;
using System.IO.Ports;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.Communication
{
    public static class DMXCommunication
    {
        // ======================
        // STANDARD DMX FRAME
        // ======================
        public static void SendDMXFrame(string comPort, byte[] data)
        {
            try
            {
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

        // ======================
        // FIXED ELO FRAME
        // ======================
        public static void SendELOFrame(string comPort, byte[] channelBytes)
        {
            try
            {
                using var sp = new SerialPort(comPort, 250000, Parity.None, 8, StopBits.Two);
                sp.Handshake = Handshake.None;
                sp.Open();

                // Give adapter time to settle
                Thread.Sleep(20);

                byte[] start = { 0x00, 0xFF, 0x00 };
                byte[] stop = { 0xFF, 0xF0, 0xF0 };

                // Build complete frame: START + DATA + STOP
                byte[] frame = new byte[start.Length + channelBytes.Length /* + stop.Length*/];
                Array.Copy(start, 0, frame, 0, start.Length);
                Array.Copy(channelBytes, 0, frame, start.Length, channelBytes.Length);
                //Array.Copy(stop, 0, frame, start.Length + channelBytes.Length, stop.Length);

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
