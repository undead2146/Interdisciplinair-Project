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

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string? Manufacturer
        {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }


        public string? ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }
        // ✅ TOEGEVOEGD: Publieke property om de DmxDivisions op te slaan. 
        // Dit lost de fout CS1061 op.
        [ObservableProperty]
        private int _dmxDivisions = 255;

        public string? ImageBase64 { get; set; }

        public string? ComPort
        {
            get => _comPort;
            set => SetProperty(ref _comPort, value);
        }

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
            if (string.IsNullOrWhiteSpace(json))
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<Fixture>(json, options);

            if (parsed != null)
            {
                Name = parsed.Name ?? string.Empty;
                Manufacturer = parsed.Manufacturer ?? "None";

                Channels.Clear();
                if (parsed.Channels != null)
                {
                    foreach (var c in parsed.Channels)
                    {
                        c.TestCommand = new RelayCommand(() => SendChannelValue(c));
                        Channels.Add(c);
                    }
                }

                ImageBase64 = parsed.ImageBase64;
            }
        }

        // -------------------------------
        // Refresh available COM ports (only free ports)
        // -------------------------------
        private void RefreshAvailablePorts()
        {
            var ports = SerialPort.GetPortNames();
            var freePorts = new List<string>();

            foreach (var port in ports)
            {
                try
                {
                    using var sp = new SerialPort(port);
                    sp.Open();
                    sp.Close();
                    freePorts.Add(port);
                }
                catch
                {
                    // Port is busy, skip
                }
            }

            AvailablePorts.Clear();
            foreach (var port in freePorts)
                AvailablePorts.Add(port);
        }

        // -------------------------------
        // Validate channel value (0–255 integer)
        // -------------------------------
        private bool ValidateChannel(Channel channel, out string error)
        {
            error = string.Empty;

            if (!int.TryParse(channel.Parameter.ToString(), out int val))
            {
                error = channel.Name ?? "Unknown";
                return false;
            }

            if (val < 0 || val > 255)
            {
                error = channel.Name ?? "Unknown";
                return false;
            }

            return true;
        }

        // -------------------------------
        // Send single channel (per-channel Test)
        // -------------------------------
        public void SendChannelValue(Channel channel)
        {
            if (string.IsNullOrWhiteSpace(ComPort))
            {
                MessageBox.Show("No COM port selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ValidateChannel(channel, out string invalidChannel))
            {
                MessageBox.Show(
                    $"Channel '{invalidChannel}' has an invalid value. Must be integer 0–255.",
                    "Invalid Channel Value",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var data = new List<byte> { 0x00, 0xFF, 0x00 };

            foreach (var ch in Channels)
                data.Add(ch == channel ? (byte)ch.Parameter : (byte)0x00);

            data.AddRange(new byte[] { 0xFF, 0xF0, 0xF0 });

            SendFrame(data.ToArray());
        }

        // -------------------------------
        // Send all channels sequentially (Test All)
        // -------------------------------
        private void SendAllChannels()
        {
            if (string.IsNullOrWhiteSpace(ComPort))
            {
                MessageBox.Show("No COM port selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> invalidChannels = new List<string>();
            foreach (var ch in Channels)
            {
                if (!ValidateChannel(ch, out string invalidChannel))
                    invalidChannels.Add(invalidChannel);
            }

            if (invalidChannels.Count > 0)
            {
                MessageBox.Show(
                    $"The following channels have invalid values (0–255): {string.Join(", ", invalidChannels)}",
                    "Invalid Channel Values",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var data = new List<byte> { 0x00, 0xFF, 0x00 };
            foreach (var ch in Channels)
                data.Add((byte)ch.Parameter);

            data.AddRange(new byte[] { 0xFF, 0xF0, 0xF0 });

            try
            {
                SendFrame(data.ToArray(), showMessage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send frame: {ex.Message}", "Send Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------------
        // Send frame over serial (temporary 115200 baud, non-blocking)
        // -------------------------------
        private void SendFrame(byte[] data, bool showMessage = true)
        {
            try
            {
                using var sp = new SerialPort(ComPort!, 115200, Parity.None, 8, StopBits.One);
                sp.Open();
                sp.Write(data, 0, data.Length);
                sp.Close();

                if (showMessage)
                {
                    string hexString = BitConverter.ToString(data).Replace("-", " ");
                    MessageBox.Show(
                        $"Sent {data.Length} bytes to {ComPort} at 115200 baud:\n{hexString}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending data: {ex.Message}", "Serial Port Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
