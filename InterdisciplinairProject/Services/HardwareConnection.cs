using InterdisciplinairProject.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Services
{
    public class HardwareConnection : IHardwareConnection
    {
        private readonly string _scenesFilePath;

        public HardwareConnection()
        {
            string appFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InterdisciplinairProject");

            // Ensure the directory exists
            Directory.CreateDirectory(appFolder);

            _scenesFilePath = Path.Combine(appFolder, "scenes.json");

            // Optional: Copy the default scenes.json from your project if it doesn't exist yet
            string defaultScenesPath = Path.Combine(
                AppContext.BaseDirectory,
                "InterdisciplinairProject.Features", "Scene", "data", "scenes.json");

            if (!File.Exists(_scenesFilePath) && File.Exists(defaultScenesPath))
            {
                File.Copy(defaultScenesPath, _scenesFilePath);
            }
        }

        public async Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value)
        {
            try
            {
                // Valideer de waarde (0-255)
                if (value < 0 || value > 255)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Waarde moet tussen 0 en 255 zijn");
                }

                // Lees het scenes bestand
                if (!File.Exists(_scenesFilePath))
                {
                    throw new FileNotFoundException($"Scenes bestand niet gevonden: {_scenesFilePath}");
                }

                string jsonContent = await File.ReadAllTextAsync(_scenesFilePath);

                // Parse JSON met System.Text.Json
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                using MemoryStream stream = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                // Schrijf de aangepaste JSON
                WriteModifiedJson(doc.RootElement, writer, fixtureInstanceId, channelName, value);

                writer.Flush();

                // Schrijf terug naar bestand
                string updatedJson = Encoding.UTF8.GetString(stream.ToArray());
                await File.WriteAllTextAsync(_scenesFilePath, updatedJson);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij het updaten van kanaal: {ex.Message}");
                return false;
            }
        }

        private void WriteModifiedJson(JsonElement element, Utf8JsonWriter writer,
            string targetInstanceId, string targetChannel, byte value)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        writer.WritePropertyName(prop.Name);

                        // Check of we bij de channels property zijn van de juiste fixture
                        if (prop.Name == "channels" && IsTargetFixture(element, targetInstanceId))
                        {
                            WriteModifiedChannels(prop.Value, writer, targetChannel, value);
                        }
                        else
                        {
                            WriteModifiedJson(prop.Value, writer, targetInstanceId, targetChannel, value);
                        }
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        WriteModifiedJson(item, writer, targetInstanceId, targetChannel, value);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    writer.WriteStringValue(element.GetString());
                    break;

                case JsonValueKind.Number:
                    writer.WriteNumberValue(element.GetInt32());
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;
            }
        }

        private bool IsTargetFixture(JsonElement fixtureElement, string targetInstanceId)
        {
            if (fixtureElement.TryGetProperty("instanceId", out JsonElement instanceId))
            {
                return instanceId.GetString() == targetInstanceId;
            }
            return false;
        }

        private void WriteModifiedChannels(JsonElement channels, Utf8JsonWriter writer,
            string targetChannel, byte value)
        {
            writer.WriteStartObject();

            bool channelFound = false;
            foreach (JsonProperty channel in channels.EnumerateObject())
            {
                writer.WritePropertyName(channel.Name);

                if (channel.Name.Equals(targetChannel, StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteNumberValue(value);
                    channelFound = true;
                }
                else
                {
                    if (channel.Value.ValueKind == JsonValueKind.Null)
                        writer.WriteNullValue();
                    else if (channel.Value.ValueKind == JsonValueKind.Number)
                        writer.WriteNumberValue(channel.Value.GetInt32());
                }
            }

            // Als het kanaal nog niet bestaat, voeg het toe
            if (!channelFound)
            {
                writer.WritePropertyName(targetChannel);
                writer.WriteNumberValue(value);
            }

            writer.WriteEndObject();
        }



    }
}
