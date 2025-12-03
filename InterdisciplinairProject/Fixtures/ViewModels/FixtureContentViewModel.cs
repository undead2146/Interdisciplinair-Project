using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Fixtures.Communication;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using System;
using System.IO.Ports;
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

        // UPDATED DEFAULT METHOD
        private string? _selectedMethod = "Standard DMX";

        public event EventHandler? DeleteRequested;
        public event EventHandler? BackRequested;
        public event EventHandler<FixtureContentViewModel>? EditRequested;

        public string? Name { get => _name; set => SetProperty(ref _name, value); }
        public string? Manufacturer { get => _manufacturer; set => SetProperty(ref _manufacturer, value); }
        public string? ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }
        public string? ImageBase64 { get; set; }
        public string? ComPort { get => _comPort; set => SetProperty(ref _comPort, value); }

        // WiFi properties for ELO (WiFi)
        private string? _wifiIP;
        private int _wifiPort = 6038; // default port

        public string? WifiIP { get => _wifiIP; set => SetProperty(ref _wifiIP, value); }
        public int WifiPort { get => _wifiPort; set => SetProperty(ref _wifiPort, value); }

        // UPDATED LABELS
        public ObservableCollection<string> LampMethods { get; set; } = new()
        {
            "Standard DMX",
            "ELO (Cable)",
            "ELO (WiFi)"
        };

        public string? SelectedMethod { get => _selectedMethod; set => SetProperty(ref _selectedMethod, value); }

        public ObservableCollection<string> AvailablePorts { get; set; } = new();
        public ObservableCollection<Channel> Channels { get; set; } = new();

        public ICommand BackCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand TestAllCommand { get; }

        public FixtureContentViewModel(string json)
        {
            BackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, this));
            TestAllCommand = new RelayCommand(TestAllChannels);

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
                foreach (var channel in parsed.Channels)
                {
                    if (int.TryParse(channel.Value, out var param))
                        channel.Parameter = param;

                    channel.TestCommand = new RelayCommand(() => TestSingleChannel(channel));
                    Channels.Add(channel);
                }
            }
        }

        // ===========================
        // Testing
        // ===========================
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
            if (channel.Parameter < 0 || channel.Parameter > 1024)
            {
                error = channel.Name;
                return false;
            }

            return true;
        }

        public void TestSingleChannel(Channel channel)
        {
            if (SelectedMethod == "ELO (WiFi)" && string.IsNullOrWhiteSpace(WifiIP))
            {
                MessageBox.Show("No WiFi IP entered.");
                return;
            }

            if (SelectedMethod != "ELO (WiFi)" && string.IsNullOrWhiteSpace(ComPort))
            {
                MessageBox.Show("No COM port selected.");
                return;
            }

            if (!ValidateChannel(channel, out string invalid))
            {
                MessageBox.Show($"Invalid value on channel '{invalid}'");
                return;
            }

            // Build a small frame with just this channel
            byte[] eloData = new byte[Channels.Count];
            for (int i = 0; i < Channels.Count; i++)
                eloData[i] = (i == Channels.IndexOf(channel)) ? (byte)channel.Parameter : (byte)0;

            if (SelectedMethod == "Standard DMX")
            {
                byte[] dmx = new byte[512];
                dmx[Channels.IndexOf(channel)] = (byte)channel.Parameter;
                DMXCommunication.SendDMXFrame(ComPort!, dmx);
            }
            else if (SelectedMethod == "ELO (Cable)")
            {
                DMXCommunication.SendELOFrame(ComPort!, eloData);
            }
            else if (SelectedMethod == "ELO (WiFi)")
            {
                DMXCommunication.SendELOWifiFrame(WifiIP!, WifiPort, eloData);
            }
        }

        private void TestAllChannels()
        {
            if (SelectedMethod == "ELO (WiFi)" && string.IsNullOrWhiteSpace(WifiIP))
            {
                MessageBox.Show("No WiFi IP entered.");
                return;
            }

            if (SelectedMethod != "ELO (WiFi)" && string.IsNullOrWhiteSpace(ComPort))
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

            if (SelectedMethod == "Standard DMX")
            {
                byte[] dmx = new byte[512];
                for (int i = 0; i < Channels.Count && i < 512; i++)
                    dmx[i] = (byte)Channels[i].Parameter;

                DMXCommunication.SendDMXFrame(ComPort!, dmx);
            }
            else if (SelectedMethod == "ELO (Cable)")
            {
                byte[] eloData = new byte[Channels.Count];
                for (int i = 0; i < Channels.Count; i++)
                    eloData[i] = (byte)Channels[i].Parameter;

                DMXCommunication.SendELOFrame(ComPort!, eloData);
            }
            else if (SelectedMethod == "ELO (WiFi)")
            {
                byte[] eloData = new byte[Channels.Count];
                for (int i = 0; i < Channels.Count; i++)
                    eloData[i] = (byte)Channels[i].Parameter;

                DMXCommunication.SendELOWifiFrame(WifiIP!, WifiPort, eloData);
            }
        }
    }
}
