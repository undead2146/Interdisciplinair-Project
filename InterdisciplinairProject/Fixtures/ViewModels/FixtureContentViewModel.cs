using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Windows;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureContentViewModel : ObservableObject
    {
        private string? _name;
        private string? _manufacturer;
        private string? _imagePath;
        private string? _comPort;

        public event EventHandler? DeleteRequested;

        public event EventHandler? BackRequested;

        public event EventHandler<FixtureContentViewModel>? EditRequested;

        public string? Name { get => _name; set => SetProperty(ref _name, value); }
        public string? Manufacturer { get => _manufacturer; set => SetProperty(ref _manufacturer, value); }
        public string? ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }
        public string? ImageBase64 { get; set; }

        public string? ComPort { get => _comPort; set => SetProperty(ref _comPort, value); }

        public ObservableCollection<string> AvailablePorts { get; set; } = new();
        public ObservableCollection<Channel> Channels { get; set; } = new();

        public ICommand BackCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand TestAllCommand { get; }

        public FixtureContentViewModel(string json)
        {
            BackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, this));
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));
            TestAllCommand = new RelayCommand(SendAllChannels);

            LoadFromJson(json);
            RefreshAvailablePorts();
        }

        private void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            var parsed = JsonSerializer.Deserialize<Fixture>(json);

            if (parsed != null)
            {
                Name = parsed.Name;
                Manufacturer = parsed.Manufacturer;
                ImageBase64 = parsed.ImageBase64;

                Channels.Clear();
                foreach (var c in parsed.Channels)
                {
                    c.TestCommand = new RelayCommand(() => SendChannelValue(c));
                    Channels.Add(c);
                }
            }
        }

        private void RefreshAvailablePorts()
        {
            AvailablePorts.Clear();
            foreach (var port in SerialPort.GetPortNames())
            {
                try
                {
                    using var sp = new SerialPort(port);
                    sp.Open();
                    sp.Close();
                    AvailablePorts.Add(port);
                }
                catch { }
            }
        }

        private bool ValidateChannel(Channel channel, out string error)
        {
            error = "";
            if (channel.Parameter < 0 || channel.Parameter > 255)
            {
                error = channel.Name;
                return false;
            }
            return true;
        }

        // ===========================
        //    SEND SINGLE CHANNEL
        // ===========================
        public void SendChannelValue(Channel channel)
        {
            if (ComPort == null)
            {
                MessageBox.Show("No COM port selected.");
                return;
            }

            if (!ValidateChannel(channel, out string invalid))
            {
                MessageBox.Show($"Invalid value on channel '{invalid}'");
                return;
            }

            byte[] dmx = new byte[512];
            int index = Channels.IndexOf(channel);
            if (index >= 0) dmx[index] = (byte)channel.Parameter;

            SendDMXFrame(dmx);
        }

        // ===========================
        //    SEND ALL CHANNELS
        // ===========================
        private void SendAllChannels()
        {
            if (ComPort == null)
            {
                MessageBox.Show("No COM port selected.");
                return;
            }

            foreach (var ch in Channels)
            {
                if (!ValidateChannel(ch, out string invalid))
                {
                    MessageBox.Show($"Invalid channel: {invalid}");
                    return;
                }
            }

            byte[] dmx = new byte[512];
            for (int i = 0; i < Channels.Count && i < 512; i++)
                dmx[i] = (byte)Channels[i].Parameter;

            SendDMXFrame(dmx, true);
        }

        // ===========================
        //       TRUE DMX SIGNAL
        // ===========================
        private void SendDMXFrame(byte[] data, bool showMessage = false)
        {
            try
            {
                using var sp = new SerialPort(ComPort!, 250000, Parity.None, 8, StopBits.Two);
                sp.Open();

                // BREAK (min 88 µs)
                sp.BreakState = true;
                Thread.Sleep(1);
                sp.BreakState = false;

                // MAB (min 8 µs)
                SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(10));

                // STARTCODE
                sp.Write(new byte[] { 0x00 }, 0, 1);

                // DATA (512 bytes)
                sp.Write(data, 0, data.Length);

                sp.Close();

                if (showMessage)
                    MessageBox.Show("DMX Frame sent (512 channels).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("DMX ERROR: " + ex.Message);
            }
        }
    }
}